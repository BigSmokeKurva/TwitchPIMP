using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для MenuPage.xaml
    /// </summary>
    public partial class MenuPage : Page
    {
        private static Thread timerThread = null;
        private static readonly Dictionary<string, Page> pages = new()
        {
            {"viewersbot", new ViewersBotPage() },
            {"chatbot", new ChatBotPage() },
            {"followbot", new ChatBotPage() },
            {"tokencheck", new ChatBotPage() },
            {"acctotoken", new ChatBotPage() },
            {"bitsender", new ChatBotPage() },
            {"subbot", new ChatBotPage() },
            {"autoreg", new ChatBotPage() },
            {"adbot", new ChatBotPage() },
        };
        public MenuPage(string time)
        {
            InitializeComponent();
            timerThread = new(() => TimerThread(TimeSpan.Parse(time + ":00")));
            timerThread.Start();
        }
        private void TimerThread(TimeSpan time)
        {
            int sleepMinutes;
            Dispatcher.Invoke(() =>
            {
                Timer.Content = $"{time.Days} days {time.Hours} hours";
            });
            try
            {
                sleepMinutes = time.Minutes * 1000;
                Thread.Sleep(sleepMinutes * 60);
                time -= TimeSpan.FromMinutes(sleepMinutes / 1000);
                Dispatcher.Invoke(() =>
                {
                    Timer.Content = $"{time.Days} days {time.Hours} hours";
                });
                while (((int)time.TotalMinutes / 60) > 0)
                {
                    Thread.Sleep(3600000);
                    time -= TimeSpan.FromMinutes(60);
                    Dispatcher.Invoke(() =>
                    {
                        Timer.Content = $"{time.Days} days {time.Hours} hours";
                    });
                }
                new Thread(() => MessageBox.Show("Your license has expired! To continue using the program, you must purchase/renew a license.", "License expired!")).Start();
                Dispatcher.Invoke(() => { Application.Current.MainWindow.Close(); });
            }
            catch (ThreadInterruptedException) { return; }
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
            NavigationFrame.Navigate(pages[(string)button.Tag]);
        }
        private void Hyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        public static void UnSafeStop() => timerThread?.Interrupt();

        private void NavigationFrame_Navigated(object sender, NavigationEventArgs e) => NavigationFrame.NavigationService.RemoveBackEntry();
    }
}
