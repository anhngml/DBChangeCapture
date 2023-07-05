using DBCom;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace DBCDCService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ConfigValue? conf;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            //string path = Directory.GetCurrentDirectory();
            string path = AppDomain.CurrentDomain.BaseDirectory;
            this.conf = LoadJson(path + "service.config.json");

        }
        private ConfigValue? LoadJson(string path)
        {
            try
            {
                _logger.LogInformation(path);
                Log.Logger.Information(path);
                using (StreamReader reader = new(path)) {
                    var json = reader.ReadToEnd();
                    ConfigValue? conf = JsonConvert.DeserializeObject<ConfigValue>(json);
                    return conf;
                }
            }
            catch(Exception ex)
            {
                Log.Logger.Information(ex.Message, ex.InnerException?.Message);
                return null;
            }
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var conn = new CDCapturer(conf);
            conn.TurnOnTracking(conf, Log.Logger);
            while (!stoppingToken.IsCancellationRequested)
            {
                Log.Logger.Information($"connecting to \'{conf.server}\' as \'{conf.username}\'");
                Log.Logger.Information(conn.TestConnection(false)? "connected": "connection failed");
                try
                {
                    conn.ChangeDetect(Log.Logger).ToString();
                }
                catch (Exception ex)
                {
                    Log.Logger.Information(ex.Message);
                }

                await Task.Delay(conf.interval, stoppingToken);
            }
        }
    }
}