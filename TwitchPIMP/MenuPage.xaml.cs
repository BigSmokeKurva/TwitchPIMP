using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
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
    /// Логика взаимодействия для MenuPage.xaml
    /// </summary>
    public partial class MenuPage : Page
    {
        public MenuPage()
        {
            InitializeComponent();
            //button.DataContext = login;
        }

        private void Button_Menu_Navigate(object sender, RoutedEventArgs e)
        {
            // viewersbot
            // chatbot
            // followbot
            // tokencheck
            // acctotoken
            // bitsender
            // subbot
            // autoreg
            // adbot
            var button = (Button)sender;
            foreach (UIElement element in Menu.Children)
            {
                var btn = ((Viewbox)element).Child;
                if (btn.GetType() == typeof(Button) && !btn.IsEnabled)
                {
                    btn.IsEnabled = true;
                    break;
                }
            }
            button.IsEnabled = false;
            NavigationFrame.Navigate(new Uri(button.Tag switch
            {
                "viewersbot" => "ViewersBotPage.xaml",
                "chatbot" => "ChatBotPage.xaml",
                "followbot" => "ViewersBotPage.xaml",
                "tokencheck" => "ViewersBotPage.xaml",
                "acctotoken" => "ViewersBotPage.xaml",
                "bitsender" => "ViewersBotPage.xaml",
                "subbot" => "ViewersBotPage.xaml",
                "autoreg" => "ViewersBotPage.xaml",
                "adbot" => "ViewersBotPage.xaml",
            }, UriKind.Relative));
        }
        private void Hyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Button_Check_Update(object sender, RoutedEventArgs e)
        {
            //TODO
        }
    }
}
