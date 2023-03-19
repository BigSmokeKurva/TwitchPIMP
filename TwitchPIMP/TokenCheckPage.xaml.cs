using Leaf.xNet;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для TokenCheckPage.xaml
    /// </summary>
    public partial class TokenCheckPage : Page, INotifyPropertyChanged
    {
        private static readonly Random rnd = new();
        private static readonly List<Thread> tasks = new();
        private static readonly string clientVersion = "3040e141-5964-4d72-b67d-e73c1cf355b";
        private static readonly string clientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
        private static readonly Regex bitsBalanceRegex = new("sBalance\":(.*?),");
        private static readonly Regex subscriptionBalanceRegex = new("tionToken\":{\"balance\":(.*?),");
        private ObservableCollection<string> _good = new();
        private ObservableCollection<string> _bad = new();
        private ObservableCollection<(string, int)> _accountsWithBits = new();
        private int _bits = 0;
        private ObservableCollection<(string, int)> _accountsWithPrimes = new();
        private int _subscriptions = 0;
        private string[] _tokens = Array.Empty<string>();
        public ObservableCollection<string> Good
        {
            get => _good;
            set
            {
                _good = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> Bad
        {
            get => _bad;
            set
            {
                _bad = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<(string, int)> AccountsWithBits
        {
            get => _accountsWithBits;
            set
            {
                _accountsWithBits = value;
                OnPropertyChanged();
            }
        }
        public int Bits
        {
            get => _bits;
            set
            {
                _bits = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<(string, int)> AccountsWithPrimes
        {
            get => _accountsWithPrimes;
            set
            {
                _accountsWithPrimes = value;
                OnPropertyChanged();
            }
        }
        public int Subscriptions
        {
            get => _subscriptions;
            set
            {
                _subscriptions = value;
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
        public TokenCheckPage()
        {
            DataContext = this;
            InitializeComponent();
        }
        private void ThreadBot(string[] tokens)
        {
            string res;
            string userAgent;
            int bits;
            int subscriptions;
            using HttpRequest httpRequest = new();
            httpRequest["Accept"] = "*/*";
            httpRequest["Client-Version"] = clientVersion;
            httpRequest["Accept-Encoding"] = "deflate";
            httpRequest["Client-Id"] = clientId;
            httpRequest.ConnectTimeout = 10000;
            httpRequest.KeepAliveTimeout = 10000;
            //httpRequest.IgnoreProtocolErrors = true;
            try
            {
                foreach (var token in tokens)
                {
                    userAgent = Configuration.userAgents[rnd.Next(Configuration.userAgents.Length)];
                    httpRequest["User-Agent"] = userAgent;
                    httpRequest["Authorization"] = $"OAuth {token}";
                    try
                    {
                        res = httpRequest.Post("https://gql.twitch.tv/gql",
                            "[{\"operationName\":\"SubscriptionsManagement_SubscriptionBenefits\",\"variables\":{\"limit\":100,\"cursor\":\"1618488953\",\"filter\":\"PLATFORM\",\"platform\":\"WEB\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"dc3f022290c673f53098808f1bb5148b8b9568379d312c33a089a4e17aafd9bf\"}}},{\"operationName\":\"BitsCard_Bits\",\"variables\":{},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"fe1052e19ce99f10b5bd9ab63c5de15405ce87a1644527498f0fc1aadeff89f2\"}}}]",
                            "application/json").ToString();
                        //if (res.Contains("Unauthorized"))
                        //{
                        //    Bad.Add(token);
                        //    continue;
                        //}
                        lock (Good)
                            Good.Add(token);
                        // subscriptions
                        if (res.Contains("hasPrime\":true"))
                        {
                            subscriptions = int.Parse(subscriptionBalanceRegex.Match(res).Groups[1].Value);
                            lock (AccountsWithPrimes)
                            {
                                AccountsWithPrimes.Add((token, subscriptions));
                                Subscriptions += subscriptions;
                            }
                        }
                        // bits
                        bits = int.Parse(bitsBalanceRegex.Match(res).Groups[1].Value);
                        if (bits > 0)
                        {
                            lock (AccountsWithBits)
                            {
                                AccountsWithBits.Add((token, bits));
                                Bits += bits;
                            }
                        }
                    }
                    catch (ThreadInterruptedException) { return; }
                    catch
                    {
                        lock (Bad)
                            Bad.Add(token);
                    }
                }
            }
            catch (ThreadInterruptedException) { return; }
            catch { }
        }

        private void Button_Start(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string threads;
            string capsolverApi;
            Thread thread;
            if ((string)btn.Tag == "start")
            {
                threads = Threads.Text.Trim();
                if (string.IsNullOrEmpty(threads) || string.IsNullOrWhiteSpace(threads) || threads.Contains('\n') || threads.Contains('\t') || threads.Contains("  ") || !int.TryParse(threads, out int threadsInt) ||
                    !Tokens.Any() || threadsInt == 0)
                    return;
                try
                {
                    foreach (var batch in Tokens.Batch(Tokens.Length / threadsInt))
                    {
                        thread = new(() => ThreadBot((string[])batch));
                        thread.Start();
                        tasks.Add(thread);
                    }
                }
                catch { return; }
                new Thread(() =>
                {
                    try
                    {
                        tasks.ForEach(x => x.Join());
                        tasks.Clear();
                        Dispatcher.Invoke(() =>
                        {
                            btn.Tag = "save";
                            btn.Content = "Save";
                        });
                    }
                    catch { }
                }).Start();
                btn.Tag = "stop";
                btn.Content = "Stop";
            }
            else if ((string)btn.Tag == "stop")
            {
                btn.Tag = "stoping";
                tasks.ForEach(x => x.Interrupt());
            }
            else if ((string)btn.Tag == "save")
            {
                try
                {
                    string filepath;
                    WinForms.DialogResult result;
                    WinForms.FolderBrowserDialog dialog = new();
                    result = dialog.ShowDialog();
                    if (result != WinForms.DialogResult.OK) return;
                    filepath = dialog.SelectedPath;
                    File.WriteAllLines(filepath + "\\Valid.txt", Good);
                    File.WriteAllLines(filepath + "\\Bad.txt", Bad);
                    File.WriteAllLines(filepath + "\\AccountsWithPrimes.txt", AccountsWithPrimes.Select(x => $"{x.Item1}:{x.Item2}"));
                    File.WriteAllLines(filepath + "\\AccountsWithBits.txt", AccountsWithBits.Select(x => $"{x.Item1}:{x.Item2}"));
                    Bad.Clear();
                    Good.Clear();
                    AccountsWithBits.Clear();
                    AccountsWithPrimes.Clear();
                    Bits = 0;
                    Subscriptions = 0;
                    btn.Tag = "start";
                    btn.Content = "Start";
                }
                catch { }
            }

        }
        private void Button_Upload_Tokens(object sender, RoutedEventArgs e)
        {
            if (tasks.Any()) return;

            string filepath;
            bool? result;
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Document",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };
            result = dialog.ShowDialog();
            if (result is null || !(bool)result) return;

            filepath = dialog.FileName;
            Tokens = File.ReadAllLines(filepath)
                        .Select(x => x.Trim())
                        .Where(x =>
                                        !(string.IsNullOrEmpty(x)
                                        || string.IsNullOrWhiteSpace(x)
                                        || x.Contains('\t')
                                        || x.Contains(' '))).ToArray();
        }
        public static void UnSafeStop() => tasks.ForEach(x => x.Interrupt());

    }
}
