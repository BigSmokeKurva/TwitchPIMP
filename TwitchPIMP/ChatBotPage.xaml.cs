using Leaf.xNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
    /// Логика взаимодействия для ChatBotPage.xaml
    /// </summary>
    public partial class ChatBotPage : Page, INotifyPropertyChanged
    {
        private static readonly List<Thread> tasks = new();
        private string channel;
        private bool useProxy;
        private int _good = 0;
        private int _bad = 0;
        private ProxyClient[] _proxies = Array.Empty<ProxyClient>();
        private string[] _tokens = Array.Empty<string>();
        public int Good
        {
            get => _good;
            set
            {
                _good = value;
                OnPropertyChanged();
            }
        }
        public int Bad
        {
            get => _bad;
            set
            {
                _bad = value;
                OnPropertyChanged();
            }
        }
        public ProxyClient[] Proxies
        {
            get => _proxies;
            set
            {
                _proxies = value;
                OnPropertyChanged();
            }
        }
        public string[] Tokens
        {
            get => _tokens;
            set
            {
                _tokens = value;
                OnPropertyChanged();
            }
        }
        #region Обработчик изменений переменных
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        #endregion

        public ChatBotPage()
        {
            DataContext = this;
            InitializeComponent();
        }
    }
}
