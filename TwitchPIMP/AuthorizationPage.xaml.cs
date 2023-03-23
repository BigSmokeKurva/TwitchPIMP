using System.Diagnostics;
using System.Management;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationPage.xaml
    /// </summary>
    public partial class AuthorizationPage : Page
    {
        public AuthorizationPage()
        {
            InitializeComponent();
            KeyTextBox.Text = Configuration.authorization.key;
            if (Configuration.authorization.key.Length != 0)
                SaveKey.IsChecked = true;
            //#if DEBUG
            //((Frame)Application.Current.MainWindow.FindName("NavigationFrame")).Navigate(new Uri("MenuPage.xaml", UriKind.Relative));
            //#endif

        }
        private void Hyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private static string GetHWID()
        {
            var mbs = new ManagementObjectSearcher("Select ProcessorId From Win32_processor");
            ManagementObjectCollection mbsList = mbs.Get();
            string id = "";
            foreach (ManagementObject mo in mbsList)
            {
                id = mo["ProcessorId"].ToString();
                break;
            }
            return id;
        }
        private static string Authorization(string licenseKey)
        {
            byte[] data;
            NetworkStream stream;
            int bytes;
            using TcpClient tcpClient = new();
            tcpClient.ReceiveTimeout = 5000;
            tcpClient.SendTimeout = 5000;
            tcpClient.Connect(Configuration.ip, Configuration.port);
            data = Encoding.UTF8.GetBytes($"CheckLicense_{licenseKey}_{GetHWID()}");
            stream = tcpClient.GetStream();
            stream.Write(data, 0, data.Length);
            data = new byte[512];
            bytes = stream.Read(data, 0, data.Length);
            return Encoding.UTF8.GetString(data, 0, bytes); ;
        }
        private void Button_Sign_In(object sender, RoutedEventArgs e)
        {
            bool saveKey = (bool)SaveKey.IsChecked;
            string key = KeyTextBox.Text.Trim();
            AuthorizationResponse response;
            ErrorLabel.Content = string.Empty;

            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                ErrorLabel.Content = "Enter the key!";
                KeyTextBox.Text = string.Empty;
                return;
            }
            try
            {
                // Проверка ключа, обновлений
                response = System.Text.Json.JsonSerializer.Deserialize<AuthorizationResponse>(Authorization(key));
                if (!response.is_active)
                {
                    if (response.response == "Hardware binding error!")
                    {
                        ErrorLabel.Content = "Hardware binding error!";
                        KeyTextBox.Text = string.Empty;
                    }
                    else if (response.response == "There is no such key!")
                    {
                        ErrorLabel.Content = "There is no such key!";
                        KeyTextBox.Text = string.Empty;
                    }
                    else
                    {
                        ErrorLabel.Content = "License expired!";
                        KeyTextBox.Text = string.Empty;
                    }
                    return;
                }
                if (Configuration.version != response.config.version)
                {
                    if (MessageBox.Show("Your version is out of date! Do you want to upgrade?", "Update", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(response.config.invalid_version_msg) { UseShellExecute = true });
                    }
                }
                if (saveKey)
                {
                    Configuration.authorization.key = key;
                    Configuration.Save();
                }
                else
                {
                    Configuration.authorization.key = string.Empty;
                    Configuration.Save();
                    Configuration.authorization.key = key;
                }
                ((Frame)Application.Current.MainWindow.FindName("NavigationFrame")).Navigate(new MenuPage(response.license_time_left));
            }
            catch
            {
                ErrorLabel.Content = "Authorization server error!";
            }
        }
        private void TextBox_Key_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Button_Sign_In(sender, new());
        }
    }
}
