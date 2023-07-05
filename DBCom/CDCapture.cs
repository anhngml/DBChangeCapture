using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Numerics;
using System.Text;

namespace DBCom
{
    public class CDCapturer
    {
        //private string _server;
        //private string _username;
        //private string _password;
        private ConfigValue _conf;
        //private string _capQueryString {
        //    get{
        //        var res = 
        //        $@"
        //        SELECT
        //            CT.{idCol}, CT.SYS_CHANGE_OPERATION,
        //            CT.SYS_CHANGE_COLUMNS, CT.SYS_CHANGE_CONTEXT
        //        FROM
        //            CHANGETABLE(CHANGES production.products, {syncVersion}) AS CT;
        //        ";
        //        return res;
        //    }
        //}
        //private string idCol = "product_id";
        private string getCapQueryString(string table)
        {
            return
                 $@"
                SELECT
                    *
                FROM
                    CHANGETABLE(CHANGES {table}, {getSyncVersion(table)}) AS CT;
                ";
        }
        private string getNewDataQueryString(string table, string keyField, object[] ids)
        {
            return
                 $@"
                SELECT
                    *
                FROM
                    {table}
                WHERE {keyField} IN ({string.Join(",", ids)});
                ";
        }
        private string turnOnDBTrackingQueryString(string catalog)
        {
            return $"ALTER DATABASE {catalog} SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON)";            
        }
        private string turnOnTableTrackingQueryString(string table)
        {
            return $"ALTER TABLE {table} ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)";
        }
        private BigInteger getSyncVersion(string table)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[$"syncVersion_{table}"] != null)
            {
                string val = config.AppSettings.Settings[$"syncVersion_{table}"].Value;
                return BigInteger.Parse(val);
            }
            else
                return new BigInteger(0);
        }
        private void setSyncVersion(string table, BigInteger value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[$"syncVersion_{table}"] == null)
                    config.AppSettings.Settings.Add($"syncVersion_{table}", value.ToString());
                else
                    config.AppSettings.Settings[$"syncVersion_{table}"].Value = value.ToString();
            config.Save(ConfigurationSaveMode.Modified, true);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public CDCapturer(ConfigValue conf)
        {
            this._conf = conf;
        }
        private string getConnectionString()
        {
            return $"Data Source={this._conf.server};" +
            $"Initial Catalog={this._conf.initCatalog};" +
            $"User id={this._conf.username};" +
            $"Password={this._conf.password};";
        }
        public async Task<Object> ChangeDetect(Serilog.ILogger logger)
        {
            object res = 0;
            var connectionString = getConnectionString();
            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                try
                {
                    var stringBuilder = new StringBuilder();
                    for (int t = 0; t < _conf.capTables.Length; t++)
                    {
                        var tab = _conf.capTables[t];
                        var key = _conf.keyFields[t];
                        SqlCommand command = new SqlCommand(getCapQueryString(tab), connection);
                        List<object> rows = new List<object>();
                        var changeTyp = new Dictionary<object, object>();
                        var latestVersion = getSyncVersion(tab);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var r = reader[key] != null ? reader[key] : null;
                                var typ = reader["SYS_CHANGE_OPERATION"] != null ? reader["SYS_CHANGE_OPERATION"] : null;
                                BigInteger currVersion = reader["SYS_CHANGE_VERSION"] != null ? BigInteger.Parse(reader["SYS_CHANGE_VERSION"].ToString()) : latestVersion;
                                if (currVersion > latestVersion)
                                    latestVersion = currVersion;
                                if (r != null && typ != null)
                                {
                                    rows.Add(r);
                                    changeTyp.Add(r, typ);
                                }
                            }

                            //logger.Information(string.Join(",", rows));
                        }

                        if(rows.Count < 1)
                        {
                            continue;
                        }

                        if (t == 0)
                        {
                            stringBuilder.Append("{" + $"\"token\":\"{_conf.token}\", ");
                        }
                        stringBuilder.Append($"\"{tab}\":" + "[");

                        command = new SqlCommand(getNewDataQueryString(tab, key, rows.ToArray()), connection);

                        var changedRecords = new List<IDataRecord>();

                        using(SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = (IDataRecord)reader;
                                var fieldCount = record.FieldCount;

                                if(changedRecords.Count > 0)
                                {
                                    stringBuilder.Append(", ");
                                }
                                stringBuilder.Append("{");
                                stringBuilder.Append($"\"changeOperation\":\"{changeTyp[reader[key]]}\"");

                                for(int i = 0; i < fieldCount; i++)
                                {
                                    if(i == 0)
                                        stringBuilder.Append(", ");
                                    if (record[i] != null && !string.IsNullOrEmpty(record[i].ToString()))
                                    {
                                        stringBuilder.Append($"\"{reader.GetName(i)}\":\"{record[i]}\"");
                                        stringBuilder.Append(", ");
                                    }
                                }
                                if (stringBuilder[stringBuilder.Length - 2] == ',')
                                {
                                    stringBuilder.Remove(stringBuilder.Length - 2, 2);
                                }
                                stringBuilder.Append( "}");
                                changedRecords.Add(record);
                            }
                        }
                        stringBuilder.Append($"]");
                        if(t < _conf.capTables.Length - 1)
                        {
                            stringBuilder.Append(", ");
                        }
                        setSyncVersion(tab, latestVersion);
                    }

                    if(stringBuilder.Length > 0)
                    {
                        stringBuilder.Append("}");
                    }

                    //TODO: send the changed data to the consumer endpoint
                    if(stringBuilder.Length > 0)
                    {
                        Console.WriteLine($"{DateTime.Now} - {stringBuilder}");
                        logger.Information(stringBuilder.ToString());
                        var response = await SinkConnect.GetInstance(_conf.sinkEndpoint).Push(stringBuilder.ToString());
                        Console.WriteLine($"{DateTime.Now} [Sink] - {response}");
                        logger.Information($"Server response: {response}");
                    }


                }
                finally { connection.Close(); }

                
                return res;
            }
        }
        public bool TestConnection(bool saveProfile = true)
        {
            var res = false;

            string connectionString = getConnectionString(); 

            using (SqlConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();
                    res = connection.State == System.Data.ConnectionState.Open;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    res = false;
                }

            }

            if (saveProfile)
            {
                var conf = Config.GetConf().configValue;
                conf.server = this._conf.server;
                conf.username = this._conf.username;
                conf.password = this._conf.password;
                conf.initCatalog = this._conf.initCatalog;
                conf.capTables = this._conf.capTables;
                conf.keyFields = this._conf.keyFields;
                conf.token = this._conf.token;
                Config.Save(conf);
            }

            return res;
        }
        public async void TurnOnTracking(ConfigValue conf, Serilog.ILogger logger) {
            var connectionString = getConnectionString();
            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();
                try
                {
                    try
                    {
                        SqlCommand command = new SqlCommand(turnOnDBTrackingQueryString(conf.initCatalog), connection);
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (Exception e)
                    {
                        logger.Information(e.Message);
                    }

                    foreach (var item in conf.capTables)
                    {
                        try
                        {
                            SqlCommand command1 = new SqlCommand(turnOnTableTrackingQueryString(item), connection);
                            await command1.ExecuteNonQueryAsync();
                        }
                        catch (Exception e)
                        {
                            logger.Information(e.Message);
                        }
                    }
                }
                finally { connection.Close(); }
            }
        }
    }
}