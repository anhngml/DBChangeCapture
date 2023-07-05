using DBCom;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DBChangeCaptureWS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Config conf;
        public MainWindow()
        {
            InitializeComponent();
            this.conf = Config.GetConf();
            this.txtServer.Text = this.conf.configValue.server;
            this.txtUsername.Text = this.conf.configValue.username;
            this.txtPassword.Password = this.conf.configValue.password;
            this.txtCatalog.Text = this.conf.configValue.initCatalog;
            this.txtCapTables.Text = string.Join(",",this.conf.configValue.capTables);
            this.txtKeyFields.Text = string.Join(",", this.conf.configValue.keyFields);

            this.txtToken.Text = this.conf.configValue.token;

            SetServiceBtnStatus();

            this.btnTestConnection.Click += BtnTestConnection_Click;

            this.txtServer.TextChanged += configChanged;
            this.txtUsername.TextChanged += configChanged;
            this.txtPassword.PasswordChanged += TxtPassword_PasswordChanged;

            this.txtCatalog.TextChanged += configChanged;
            this.txtCapTables.TextChanged += configChanged;
            this.txtKeyFields.TextChanged += configChanged;

            this.txtToken.TextChanged += configChanged;

            this.btnCancel.Click += BtnCancel_Click;
            this.btnStart.Click += BtnStart_Click;
        }

        private void CmbInitCatalog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            configChanged(sender, null);
        }

        private void CmbCapTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            configChanged(sender, null);
        }

        private void SetServiceBtnStatus()
        {
            this.btnStartCaption.Text = DBCServiceController.GetController(conf).IsServiceRunning() ? 
                "Stop" : 
                "Start listening";
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (DBCServiceController.GetController(conf).IsServiceRunning())
                DBCServiceController.GetController(conf).StopService(1000);
            else
            {
                var conn = new CDCapturer(this.conf.configValue);
                var res = conn.TestConnection();
                if (!res)
                {
                    this.SetConnectionTestState(res);
                }
                else
                {
                    DBCServiceController.GetController(conf).StartService(1000);
                }
            }
                
            SetServiceBtnStatus();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            configChanged(sender, null);
        }

        private void configChanged(object sender, TextChangedEventArgs e)
        {
            this.conf.configValue.server = this.txtServer.Text;
            this.conf.configValue.username = this.txtUsername.Text;
            this.conf.configValue.password = this.txtPassword.Password;
            this.conf.configValue.initCatalog = this.txtCatalog.Text;
            this.conf.configValue.capTables = this.txtCapTables.Text.Split(new char[] { ','}, System.StringSplitOptions.RemoveEmptyEntries);
            this.conf.configValue.keyFields = this.txtKeyFields.Text.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

            this.conf.configValue.token = this.txtToken.Text;
        }

        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (this.conf.configValue.capTables.Length != this.conf.configValue.keyFields.Length)
            {
                MessageBox.Show("Tables and KeyFields size must be the same", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.SetConnectionTestState(false);
            }
            else
            {
                var conn = new CDCapturer(this.conf.configValue);
                var res = conn.TestConnection();
                this.SetConnectionTestState(res);
            }
        }

        private void SetConnectionTestState(bool res)
        {
            if(res)
            {
                this.tbConnectionStatus.Text = "Successfully";
                this.tbConnectionStatus.Foreground = new SolidColorBrush(Colors.Green);
                this.tbConnectionStatus.Visibility = Visibility.Visible;
            }
            else
            {
                this.tbConnectionStatus.Text = "Failed";
                this.tbConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);
                this.tbConnectionStatus.Visibility = Visibility.Visible;
            }
        }
    }
}
