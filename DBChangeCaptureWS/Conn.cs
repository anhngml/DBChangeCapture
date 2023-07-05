using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows;
using System.Configuration;
//using Microsoft.Data.SqlClient;

namespace DBChangeMonitorWS
{
    internal class Conn
    {
        private string _server;
        private string _username;
        private string _password;
        public Conn(string server, string username, string password) {
            this._server = server;
            this._username = username;
            this._password = password;
        }
        public Conn(Config conf)
        {
            this._server = conf.server;
            this._username = conf.username;
            this._password = conf.password;
        }
        public bool TestConnection(bool saveProfile=true) {
            var res = false;

            string connectionString =
            $"Data Source={this._server};" +
            "Initial Catalog=ecus;" +
            $"User id={this._username};" +
            $"Password={this._password};";

            using (SqlConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();
                    res = connection.State == System.Data.ConnectionState.Open;
                }
                catch (Exception ex) { 
                    Console.WriteLine(ex.ToString());
                    res = false;
                }

            }

            if(saveProfile)
            {
                var conf = Config.GetConf();
                conf.server = this._server;
                conf.username = this._username;
                conf.password = this._password;
                Config.Save(conf);
            }

            return res;
        }
    }
}
