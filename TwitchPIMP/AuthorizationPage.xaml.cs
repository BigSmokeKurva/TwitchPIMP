using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }
        private void Hyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Button_Sign_In(object sender, RoutedEventArgs e)
        {
            // TODO
            bool saveKey = (bool)SaveKey.IsChecked;
            string key = KeyTextBox.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            ((Frame)Application.Current.MainWindow.FindName("NavigationFrame")).Navigate(new Uri("MenuPage.xaml", UriKind.Relative));
        }

        private void TextBox_Key_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Button_Sign_In(sender, new());
        }
    }
}
