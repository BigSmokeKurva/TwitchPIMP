using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Timer = System.Timers.Timer;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для ChatBotPage.xaml
    /// </summary>
    public partial class ChatBotPage : Page, INotifyPropertyChanged
    {
        private static readonly List<Task> tasks = new();
        private string channel;
        private bool useProxy;
        private static readonly string[] proxyTypes = new[] { "http", "socks4", "socks5" };
        private static CancellationTokenSource cancelTokenSource = null;
        private static Timer cooldownTimer = new()
        {
            Interval = 1000,
            AutoReset = true,
            Enabled = false
        };
        private static readonly Dictionary<int, DateTime> cooldownMessages = new();
        private static readonly Random rnd = new();
        private static readonly string chars = "abcdefghijklmnopqrstuvwxyz";
        private static readonly ArraySegment<byte> segmentCommands = new(Encoding.UTF8.GetBytes("CAP REQ :twitch.tv/tags twitch.tv/commands"));
        private static readonly TimeSpan timeSpan = TimeSpan.FromSeconds(20);
        private static readonly Uri uri = new("wss://irc-ws.chat.twitch.tv");
        private int _good = 0;
        private int _bad = 0;
        private string[][] advancedMessages = Array.Empty<string[]>();
        private IEnumerable<int> advancedMessagesIndexRange;
        private string[] spamMessages = Array.Empty<string>();
        private WebProxy[] _proxies = Array.Empty<WebProxy>();
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
        public WebProxy[] Proxies
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
            cooldownTimer.Elapsed += CooldownTimer;
        }
        private static string GenerateNickname()
        {
            int length = rnd.Next(5, 10);
            StringBuilder builder = new(length);
            for (int i = 0; i < length; i++)
                builder.Append(chars[rnd.Next(chars.Length)]);

            return builder.ToString();
        }
        private string[] GetAdvancedMessage()
        {
            int index = advancedMessagesIndexRange.First(x => !cooldownMessages.ContainsKey(x));
            cooldownMessages[index] = DateTime.Now.AddSeconds(rnd.Next(Configuration.chatbot.advanced_messages_cooldown_interval.Item1, Configuration.chatbot.advanced_messages_cooldown_interval.Item2));
            return advancedMessages[index];
        }
        private static void CooldownTimer(Object source, ElapsedEventArgs e)
        {
            foreach (var keyValuePair in cooldownMessages)
                if (keyValuePair.Value <= DateTime.Now)
                    cooldownMessages.Remove(keyValuePair.Key);
        }
        private async Task SendMessage(string token, string channel, string message, bool useProxy)
        {
            ArraySegment<byte> segmentJoin = new(Encoding.UTF8.GetBytes("JOIN " + channel));
            using CancellationTokenSource cancelTokenSource = new();
            try
            {
                using ClientWebSocket clientWebSocket = new();
                clientWebSocket.Options.Proxy = useProxy ? Proxies[rnd.Next(0, Proxies.Length)] : null;
                clientWebSocket.Options.KeepAliveInterval = timeSpan;
                await clientWebSocket.ConnectAsync(uri, cancelTokenSource.Token);
                await clientWebSocket.SendAsync(segmentCommands, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PASS oauth:" + token)), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"NICK {GenerateNickname()}")), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                await clientWebSocket.SendAsync(segmentJoin, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"PRIVMSG #{channel} :{message}")), WebSocketMessageType.Text, true, cancelTokenSource.Token);
            }
            catch { }
        }
        private async Task ThreadBot()
        {
            ArraySegment<byte> segmentJoin = new(Encoding.UTF8.GetBytes("JOIN " + channel));

            await Task.Delay(rnd.Next(Configuration.chatbot.thread_start_delay_interval.Item1, Configuration.chatbot.thread_start_delay_interval.Item2), cancelTokenSource.Token);
            while (!cancelTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Advanced
                    if (cooldownMessages.Count != advancedMessages.Length)
                    {
                        using ClientWebSocket clientWebSocket = new();
                        clientWebSocket.Options.Proxy = useProxy ? Proxies[rnd.Next(0, Proxies.Length)] : null;
                        clientWebSocket.Options.KeepAliveInterval = timeSpan;
                        await clientWebSocket.ConnectAsync(uri, cancelTokenSource.Token);
                        await clientWebSocket.SendAsync(segmentCommands, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                        await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PASS oauth:" + Tokens[rnd.Next(0, Tokens.Length)])), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                        await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"NICK {GenerateNickname()}")), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                        await clientWebSocket.SendAsync(segmentJoin, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                        foreach (var message in GetAdvancedMessage())
                        {
                            await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"PRIVMSG #{channel} :{message}")), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            Good++;
                            await Task.Delay(rnd.Next(Configuration.chatbot.delay_interval_after_advanced_messages.Item1, Configuration.chatbot.delay_interval_after_advanced_messages.Item2), cancelTokenSource.Token);
                        }
                    }
                    // Spam
                    for (int i = 0; i < rnd.Next(Configuration.chatbot.amount_spam_interval.Item1, Configuration.chatbot.amount_spam_interval.Item2); i++)
                    {
                        try
                        {
                            using ClientWebSocket clientWebSocket = new();
                            clientWebSocket.Options.Proxy = useProxy ? Proxies[rnd.Next(0, Proxies.Length)] : null;
                            clientWebSocket.Options.KeepAliveInterval = timeSpan;
                            await clientWebSocket.ConnectAsync(uri, cancelTokenSource.Token);
                            await clientWebSocket.SendAsync(segmentCommands, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PASS oauth:" + Tokens[rnd.Next(0, Tokens.Length)])), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"NICK {GenerateNickname()}")), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            await clientWebSocket.SendAsync(segmentJoin, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PRIVMSG #" + channel + " :" + spamMessages[rnd.Next(0, spamMessages.Length)])), WebSocketMessageType.Text, true, cancelTokenSource.Token);
                            Good++;
                            await Task.Delay(rnd.Next(Configuration.chatbot.delay_interval_after_spam_messages.Item1, Configuration.chatbot.delay_interval_after_spam_messages.Item2), cancelTokenSource.Token);
                        }
                        catch (TaskCanceledException) { return; }
                        catch { Bad++; }
                    }
                }
                catch (TaskCanceledException) { return; }
                catch { Bad++; }

            }
        }
        private void Button_Send(object sender, RoutedEventArgs e)
        {
            if (SenderTokens.Items.Count == 0) return;
            string nickname;
            string token;
            string message;
            bool useProxy;
            nickname = Nickname.Text.Trim();
            useProxy = (bool)UseProxy.IsChecked;
            token = ((string)((ComboBoxItem)SenderTokens.SelectedItem).Content).Split(' ')[1];
            message = SenderMessage.Text.Trim();
            if (string.IsNullOrEmpty(nickname) || string.IsNullOrWhiteSpace(nickname) || nickname.Contains('\n') || nickname.Contains('\t') || nickname.Contains("  ") ||
                string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message) ||
                (useProxy && !Proxies.Any()))
                return;
            Task.Run(async () =>
            {
                await SendMessage(token, nickname, message, useProxy);
                Dispatcher.Invoke(() =>
                {
                    SenderMessage.Text = string.Empty;
                });
            });
        }
        private void Button_Start(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string nickname;
            string threads;
            if ((string)btn.Tag == "start")
            {
                nickname = Nickname.Text.Trim();
                threads = Threads.Text.Trim();
                useProxy = (bool)UseProxy.IsChecked;
                if (string.IsNullOrEmpty(nickname) || string.IsNullOrWhiteSpace(nickname) || nickname.Contains('\n') || nickname.Contains('\t') || nickname.Contains("  ") ||
                    string.IsNullOrEmpty(threads) || string.IsNullOrWhiteSpace(threads) || threads.Contains('\n') || threads.Contains('\t') || threads.Contains("  ") || !int.TryParse(threads, out int threadsInt) ||
                    (useProxy && !Proxies.Any()) || !Tokens.Any())
                    return;
                channel = nickname;
                cancelTokenSource = new CancellationTokenSource();
                cooldownTimer.Start();
                for (int i = 0; i < threadsInt; i++)
                    tasks.Add(ThreadBot());
                new Thread(async () =>
                {
                    await Task.WhenAll(tasks);
                }).Start();
                btn.Tag = "stop";
                btn.Content = "Stop";

            }
            else if ((string)btn.Tag == "stop")
            {
                btn.Tag = "stoping";
                new Thread(() =>
                {
                    try
                    {
                        cancelTokenSource.Cancel();
                        while (tasks.Any(x => x.Status == TaskStatus.Running))
                            Thread.Sleep(100);
                        Bad = 0;
                        Good = 0;
                        tasks.Clear();
                        cooldownTimer.Stop();
                        cooldownMessages.Clear();
                        Dispatcher.Invoke(() =>
                        {
                            btn.Tag = "start";
                            btn.Content = "Start";
                        });
                    }
                    catch { }
                }).Start();
            }

        }
        private void Button_Upload_Proxies(object sender, RoutedEventArgs e)
        {
            // HTTP or any: 188.130.143.204:5500@31254134:ProxySoxybot or 188.130.143.204:5500
            // Auto: http://188.130.143.204:5500@31254134:ProxySoxybot or http://188.130.143.204:5500
            if (tasks.Any()) return;
            List<WebProxy> proxies = new();
            string proxyType = ProxyType.Text;
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
            if (proxyType != "auto")
                foreach (var line in File.ReadAllLines(filepath)
                                        .Select(x => x.Trim())
                                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t') || proxyTypes.Any(y => x.StartsWith(y))) && x.Contains(':')))
                {
                    try
                    {
                        if (line.Contains('@') && line.Count(x => x == ':') == 2)
                            proxies.Add(new WebProxy
                            {
                                Address = new($"{proxyType}://{line.Split('@')[0]}"),
                                Credentials = new NetworkCredential()
                                {
                                    UserName = line.Split('@')[1].Split(':')[0],
                                    Password = line.Split('@')[1].Split(':')[1],
                                }
                            });
                        else if (line.Count(x => x == ':') == 1)
                            proxies.Add(new WebProxy
                            {
                                Address = new($"{proxyType}://{line}"),
                            });
                    }
                    catch { }
                }
            else if (proxyType == "auto")
                foreach (var line in File.ReadAllLines(filepath)
                        .Select(x => x.Trim())
                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t')) && proxyTypes.Any(y => x.StartsWith(y)) && x.Contains(':')))
                {
                    try
                    {
                        if (line.Contains('@') && line.Count(x => x == ':') == 2)
                            proxies.Add(new WebProxy
                            {
                                Address = new(line.Split('@')[0]),
                                Credentials = new NetworkCredential()
                                {
                                    UserName = line.Split('@')[1].Split(':')[0],
                                    Password = line.Split('@')[1].Split(':')[1],
                                }
                            });
                        else if (line.Count(x => x == ':') == 1)
                            proxies.Add(new WebProxy
                            {
                                Address = new(line),
                            });
                    }
                    catch { }
                }
            Proxies = proxies.ToArray();
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
            SenderTokens.Items.Clear();
            for (var i = 0; i < Tokens.Length; i++)
                SenderTokens.Items.Add(new ComboBoxItem() { Content = $"{i}: {Tokens[i]}" });
            SenderTokens.SelectedIndex = 0;
        }
        private void Button_Upload_Advanced_Messages(object sender, RoutedEventArgs e)
        {
            if (tasks.Any()) return;

            List<string[]> advancedMessages = new();
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

            foreach (var line in File.ReadAllLines(filepath)
                        .Select(x => x.Trim())
                        .Where(x =>
                                        !(string.IsNullOrEmpty(x)
                                        || string.IsNullOrWhiteSpace(x)
                                        || x.Contains('\t'))))
            {
                advancedMessages.Add(line.Split('|').Where(x => x.Length > 0).ToArray());
            }
            this.advancedMessages = advancedMessages.ToArray();
            advancedMessagesIndexRange = Enumerable.Range(0, advancedMessages.Count).OrderBy(item => rnd.Next());
        }
        private void Button_Upload_Spam_Messages(object sender, RoutedEventArgs e)
        {
            if (tasks.Any()) return;

            List<string> spamMessages = new();
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

            foreach (var line in File.ReadAllLines(filepath)
                        .Select(x => x.Trim())
                        .Where(x =>
                                        !(string.IsNullOrEmpty(x)
                                        || string.IsNullOrWhiteSpace(x)
                                        || x.Contains('\t'))))
            {
                spamMessages.Add(line);
            }
            this.spamMessages = spamMessages.ToArray();
            Console.WriteLine(this.spamMessages.Length);
        }
        public static void UnSafeStop()
        {
            cancelTokenSource?.Cancel();
            cooldownTimer.Stop();
        }

    }
}
