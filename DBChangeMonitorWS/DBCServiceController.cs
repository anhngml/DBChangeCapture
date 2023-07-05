using DBCom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DBChangeMonitorWS
{
    internal class DBCServiceController
    {
        private DBCServiceController(Config conf) { 
            this.conf = conf;
        }
        private Config conf;
        private static DBCServiceController _controller;
        static string serviceName = "Ecus CDC Service V2";

        public static DBCServiceController GetController(Config conf) {
            if(_controller == null || _controller.conf != conf)
            {
                _controller = new DBCServiceController(conf);
            }
            return _controller;
        }
        public bool IsServiceRunning() {
            ServiceController service = new ServiceController(serviceName);
            try
            { 
                return service.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }
        bool DoesServiceExist()
        {
            ServiceController[] services = ServiceController.GetServices();
            var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
            return service != null;
        }
        private string _scmd(string cmd) {
            string path = Directory.GetCurrentDirectory();
            string strCmdText;
            string binPath = cmd == "create" ? " binPath= " + path + "\\DBCDCService.exe" : "";
            strCmdText = $"/C sc {cmd} \"{serviceName}\"{binPath}";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = strCmdText;

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            process.StartInfo = startInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        public void InstallService()
        {
            _scmd("create");
        }
        public void UninstallService()
        {
            _scmd("stop");
            _scmd("delete");
        }
        public void StartService(int timeoutMilliseconds)
        {
            if(!DoesServiceExist())
            {
                InstallService();
            }

            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch 
            {
            }
        }
        public void StopService(int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch
            {
            }
        }
        public void RestartService(int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch
            {
            }
        }

    }
}
