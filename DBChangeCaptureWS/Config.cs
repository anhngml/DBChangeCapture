using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace DBChangeMonitorWS
{
    internal class Config
    {
        public string server { set; get; }
        public string username { set; get; }
        public string password { set; get; }
        private Config(string server, string username, string password) {
            this.server = server;
            this.username = username;
            this.password = password;
        }
        public override bool Equals(object? obj)
        {
            if(obj != null && obj is Config)
            {
                var o = (Config)obj;
                return o.username == username && o.password == password && o.server == server;
            }
            return false;
        }
        public static Config GetConf()
        {
            return Load();        
        }
        public static void Save(Config conf)
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

            config.Save(ConfigurationSaveMode.Modified, true);
            ConfigurationManager.RefreshSection("appSettings");
            string json = JsonConvert.SerializeObject(new
            {
                server = conf.server,
                username = conf.username, 
                password = conf.password,
            }, Formatting.Indented);
            var path = Directory.GetCurrentDirectory();
            System.IO.File.WriteAllText($"{path}\\service.config.json", json);
        }
        private static Config Load() {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var conf = new Config(
                (config.AppSettings.Settings["server"] != null)? config.AppSettings.Settings["server"].Value: "",
                (config.AppSettings.Settings["username"] != null) ? config.AppSettings.Settings["username"].Value: "",
                (config.AppSettings.Settings["password"] != null) ? config.AppSettings.Settings["password"].Value: ""
                );
            return conf;
        }
    }
}
