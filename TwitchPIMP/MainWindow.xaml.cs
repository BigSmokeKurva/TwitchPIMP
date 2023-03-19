using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace TwitchPIMP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Rounded window
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(
                new WindowInteropHelper(GetWindow(this)).EnsureHandle(),
                DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref preference,
                sizeof(uint));
            Configuration.Parse();
            NavigationFrame.Navigate(new Uri("AuthorizationPage.xaml", UriKind.Relative));
        }
        private void WindowMoveEvent(object sender, MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
            {
                Point mousePosition = e.MouseDevice.GetPosition(this);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Top = mousePosition.Y;
                Application.Current.MainWindow.Left = mousePosition.X - (((Border)sender).ActualWidth / 2);
            }
            var height = Application.Current.MainWindow.Height;
            var width = Application.Current.MainWindow.Width;
            Application.Current.MainWindow.ResizeMode = ResizeMode.CanResize;
            DragMove();
            Application.Current.MainWindow.ResizeMode = ResizeMode.NoResize;

            if (Application.Current.MainWindow.WindowState != WindowState.Maximized && (Application.Current.MainWindow.Height != height || Application.Current.MainWindow.Width != width))
            {
                Application.Current.MainWindow.Height = height;
                Application.Current.MainWindow.Width = width;
            }

        }

        private void Button_Close(object sender, RoutedEventArgs e) => Close();
        private void Button_Roll(object sender, RoutedEventArgs e) => Application.Current.MainWindow.WindowState = WindowState.Minimized;
        private void Button_Maximize(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
            else
            {
                Application.Current.MainWindow.ResizeMode = ResizeMode.CanResize;
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
                Application.Current.MainWindow.ResizeMode = ResizeMode.NoResize;

            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // TODO
            MenuPage.UnSafeStop();
            ViewersBotPage.UnSafeStop();
            ChatBotPage.UnSafeStop();
            FollowBotPage.UnSafeStop();
            AutoregPage.UnSafeStop();
            TokenCheckPage.UnSafeStop();
            WarmupPage.UnSafeStop();
        }

        private void NavigationFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e) => NavigationFrame.NavigationService.RemoveBackEntry();
    }
}
