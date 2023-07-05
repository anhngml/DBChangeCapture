using Newtonsoft.Json;
using System.Configuration;
using Formatting = Newtonsoft.Json.Formatting;

namespace DBCom
{
    public class ConfigValue
    {
        public string server { set; get; }
        public string initCatalog { get; set; }
        public string[] capTables { get; set; }
        public string username { set; get; }
        public string password { set; get; }
        public string[] keyFields { get; set; }

        public string token { set; get; }
        public string sinkEndpoint { set; get; }
        public int interval { set; get; }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is ConfigValue)
            {
                var o = (ConfigValue)obj;
                return o.username == username && o.password == password && o.server == server && 
                    o.initCatalog == initCatalog && o.capTables.SequenceEqual(capTables) && 
                    o.keyFields.SequenceEqual(keyFields) && 
                    o.token == token && o.sinkEndpoint == sinkEndpoint && o.interval == interval;
            }
            return false;
        }
    }
    public class Config
    {
        public ConfigValue configValue { set; get; }
        private Config(string server, string username, string password, 
            string initCatalog, string[] capTables, string[] keyFields, string token, 
            string sinkEndpoint, int interval) {
            if(configValue == null) configValue = new ConfigValue();
            configValue.server = server;
            configValue.username = username;
            configValue.password = password;
            configValue.initCatalog = initCatalog;
            configValue.capTables = capTables;
            configValue.keyFields = keyFields;
            configValue.token = token;
            configValue.sinkEndpoint = sinkEndpoint;
            configValue.interval = interval;
        }
        public override bool Equals(object? obj)
        {
            if(obj != null && obj is Config)
            {
                var o = (Config)obj;
                return o.configValue.Equals(this.configValue);
            }
            return false;
        }
        public static Config GetConf()
        {
            return Load();        
        }
        public static void Save(ConfigValue conf)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings["server"] == null)
                config.AppSettings.Settings.Add("server", conf.server);
            else
                config.AppSettings.Settings["server"].Value = conf.server;

            if (config.AppSettings.Settings["username"] == null)
                config.AppSettings.Settings.Add("username", conf.username);
            else
                config.AppSettings.Settings["username"].Value = conf.username;

            if (config.AppSettings.Settings["password"] == null)
                config.AppSettings.Settings.Add("password", conf.password);
            else
                config.AppSettings.Settings["password"].Value = conf.password;

            if (config.AppSettings.Settings["initCatalog"] == null)
                config.AppSettings.Settings.Add("initCatalog", conf.initCatalog);
            else
                config.AppSettings.Settings["initCatalog"].Value = conf.initCatalog;

            if (config.AppSettings.Settings["capTables"] == null)
                config.AppSettings.Settings.Add("capTables", string.Join(",", conf.capTables));
            else
                config.AppSettings.Settings["capTables"].Value = string.Join(",", conf.capTables);

            if (config.AppSettings.Settings["keyFields"] == null)
                config.AppSettings.Settings.Add("keyFields", string.Join(",", conf.keyFields));
            else
                config.AppSettings.Settings["keyFields"].Value = string.Join(",", conf.keyFields);

            if (config.AppSettings.Settings["token"] == null)
                config.AppSettings.Settings.Add("token", conf.token);
            else
                config.AppSettings.Settings["token"].Value = conf.token;

            if (config.AppSettings.Settings["sinkEndpoint"] == null)
                config.AppSettings.Settings.Add("sinkEndpoint", conf.sinkEndpoint);
            else
                config.AppSettings.Settings["sinkEndpoint"].Value = conf.sinkEndpoint;

            if (config.AppSettings.Settings["interval"] == null)
                config.AppSettings.Settings.Add("interval", conf.interval.ToString());
            else
                config.AppSettings.Settings["interval"].Value = conf.interval.ToString();

            config.Save(ConfigurationSaveMode.Modified, true);
            ConfigurationManager.RefreshSection("appSettings");
            string json = JsonConvert.SerializeObject(new
            {
                server = conf.server,
                username = conf.username, 
                password = conf.password,
                initCatalog = conf.initCatalog,
                capTables = conf.capTables,
                keyFields = conf.keyFields,
                token = conf.token,
                sinkEndpoint = conf.sinkEndpoint,
                interval = conf.interval,
            }, Formatting.Indented);
            var path = Directory.GetCurrentDirectory();
            System.IO.File.WriteAllText($"{path}\\service.config.json", json);
        }
        private static Config Load() {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var conf = new Config(
                (config.AppSettings.Settings["server"] != null)? config.AppSettings.Settings["server"].Value: "",
                (config.AppSettings.Settings["username"] != null) ? config.AppSettings.Settings["username"].Value: "",
                (config.AppSettings.Settings["password"] != null) ? config.AppSettings.Settings["password"].Value: "",
                (config.AppSettings.Settings["initCatalog"] != null) ? config.AppSettings.Settings["initCatalog"].Value : "",
                (config.AppSettings.Settings["capTables"] != null) ?
                config.AppSettings.Settings["capTables"].Value.Split(new char[] {','})
                : new string[] {},
                (config.AppSettings.Settings["keyFields"] != null) ?
                config.AppSettings.Settings["keyFields"].Value.Split(new char[] { ',' })
                : new string[] { },
                (config.AppSettings.Settings["token"] != null) ? config.AppSettings.Settings["token"].Value : "",
                 (config.AppSettings.Settings["sinkEndpoint"] != null) ? config.AppSettings.Settings["sinkEndpoint"].Value : "http://103.124.93.148:6661/api/import-declarations/synchronize-data-ecus",
                (config.AppSettings.Settings["interval"] != null) ? int.Parse(config.AppSettings.Settings["interval"].Value) : 1000 * 60 * 5
                 );
            return conf;
        }
    }
}
