using Leaf.xNet;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для FollowBotPage.xaml
    /// </summary>
    public partial class FollowBotPage : Page, INotifyPropertyChanged
    {
        private static readonly Random rnd = new();
        private static readonly string[] proxyTypes = new[] { "http", "socks4", "socks5" };
        //private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly List<Thread> tasks = new();
        private static readonly string clientVersion = "3040e141-5964-4d72-b67d-e73c1cf355b";
        private static readonly string clientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
        private static readonly Regex channelIdRegex = new("\"id\":\"(.*?)\",");
        private static readonly Regex IntegrityTokenRegex = new("token\":\"(.*?)\",");
        private static string postData;
        private string capsolverApi;
        private int delay;
        private int _good = 0;
        private int _bad = 0;
        private (ProxyClient, string)[] _proxies = Array.Empty<(ProxyClient, string)>();
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
        public (ProxyClient, string)[] Proxies
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

        public FollowBotPage()
        {
            DataContext = this;
            InitializeComponent();
            CapsolverApi.Text = Configuration.other.capsolver_api;
        }
        private void ThreadBot(string[] tokens)
        {
            string res;
            HttpResponse _res;
            string deviceId;
            string integrityToken;
            string userAgent;
            (ProxyClient, string) proxy;
            KasadaResponse kasada;
            using HttpRequest httpRequest = new();
            httpRequest["Accept"] = "*/*";
            httpRequest["Client-Version"] = clientVersion;
            httpRequest["Accept-Encoding"] = "deflate";
            httpRequest["Client-Id"] = clientId;
            httpRequest.ConnectTimeout = 10000;
            httpRequest.KeepAliveTimeout = 10000;
            try
            {
                foreach (var token in tokens)
                {
                    userAgent = Configuration.userAgents[rnd.Next(Configuration.userAgents.Length)];
                    proxy = Proxies[rnd.Next(0, Proxies.Length)];
                    httpRequest["User-Agent"] = userAgent;
                    httpRequest["Authorization"] = $"OAuth {token}";
                    httpRequest.Proxy = proxy.Item1;
                    try
                    {
                        _res = httpRequest.Raw(HttpMethod.HEAD, "https://twitch.tv");
                        deviceId = _res.Cookies.GetCookies("https://twitch.tv").First(x => x.Name == "unique_id").Value;
                        kasada = SolveKasada(proxy.Item2, userAgent);
                        httpRequest["X-Device-Id"] = deviceId;
                        //httpRequest["Client-Request-Id"] = GetRandomId(32);
                        //httpRequest["Client-Session-Id"] = GetRandomId(16).ToLower();
                        httpRequest["x-kpsdk-ct"] = kasada.solution.xkpsdkct;
                        httpRequest["x-kpsdk-cd"] = kasada.solution.xkpsdkcd;
                        res = httpRequest.Post("https://gql.twitch.tv/integrity").ToString();
                        integrityToken = IntegrityTokenRegex.Match(res).Groups[1].Value;
                        //httpRequest["Client-Session-Id"] = GetRandomId(16).ToLower();
                        httpRequest["Client-Integrity"] = integrityToken;
                        res = httpRequest.Post("https://gql.twitch.tv/gql", postData, "application/json").ToString();
                        if (!res.Contains("\"id\":\"")) throw new Exception();
                        Good++;
                        Thread.Sleep(delay);
                    }
                    catch (ThreadInterruptedException) { return; }
                    catch { Bad++; }
                }
            }
            catch (ThreadInterruptedException) { return; }
            catch { Bad++; }
        }
        private static string GetChannelId(string channel)
        {
            string res;

            using HttpRequest httpRequest = new();
            httpRequest["Accept-Encoding"] = "deflate";
            httpRequest["Client-ID"] = clientId;
            res = httpRequest.Post("https://gql.twitch.tv/gql",
                "{\"operationName\": \"ChannelShell\", \"variables\": {\"login\": \"" + channel + "\"}, \"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"580ab410bcd0c1ad194224957ae2241e5d252b2c5173d8e0cce9d32d5bb14efe\"}}}"
                , "application/json").ToString();
            if (res.Contains("UserDoesNotExist")) throw new Exception();
            return channelIdRegex.Match(res).Groups[1].Value;
        }
        private KasadaResponse SolveKasada(string proxy, string userAgent)
        {
            string res;
            KasadaResponse responseJson;
            using HttpRequest httpRequest = new();
            httpRequest.ConnectTimeout = 30000;
            httpRequest.KeepAliveTimeout = 30000;
            res = httpRequest.Post("https://api.capsolver.com/kasada/invoke",
                "{\"clientKey\": \"" + capsolverApi + "\", \"task\": {\"type\": \"AntiKasadaTask\", \"pageURL\": \"https://gql.twitch.tv/\", \"proxy\": \"" + proxy + "\", \"cd\": true, \"onlyCD\": false, \"userAgent\": \"" + userAgent + "\"}}"
                , "application/json").ToString();
            responseJson = JsonSerializer.Deserialize<KasadaResponse>(res);
            if (responseJson.errorId != 0) throw new Exception();
            return responseJson;
        }
        private void Button_Start(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string nickname;
            string threads;
            string targetId;
            string delay;
            string capsolverApi;
            Thread thread;
            if ((string)btn.Tag == "start")
            {
                nickname = Nickname.Text.Trim();
                threads = Threads.Text.Trim();
                delay = ActionDelay.Text.Trim();
                capsolverApi = CapsolverApi.Text.Trim();
                if (string.IsNullOrEmpty(nickname) || string.IsNullOrWhiteSpace(nickname) || nickname.Contains('\n') || nickname.Contains('\t') || nickname.Contains("  ") ||
                    string.IsNullOrEmpty(capsolverApi) || string.IsNullOrWhiteSpace(capsolverApi) || capsolverApi.Contains('\n') || capsolverApi.Contains('\t') || capsolverApi.Contains("  ") ||
                    string.IsNullOrEmpty(threads) || string.IsNullOrWhiteSpace(threads) || threads.Contains('\n') || threads.Contains('\t') || threads.Contains("  ") || !int.TryParse(threads, out int threadsInt) ||
                    string.IsNullOrEmpty(delay) || string.IsNullOrWhiteSpace(delay) || delay.Contains('\n') || delay.Contains('\t') || delay.Contains("  ") || !int.TryParse(delay, out int delayInt) ||
                    !Proxies.Any() || !Tokens.Any() || threadsInt == 0)
                    return;
                try
                {
                    targetId = GetChannelId(nickname);
                }
                catch { return; }
                this.capsolverApi = capsolverApi;
                if (Configuration.other.capsolver_api != capsolverApi)
                {
                    Configuration.other.capsolver_api = capsolverApi;
                    Configuration.Save();
                }
                postData = FollowMode.Text == "Follow" ? "[{\"operationName\": \"FollowButton_FollowUser\", \"variables\": {\"input\": {\"disableNotifications\": false, \"targetID\": \"" + targetId + "\"}}, \"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"800e7346bdf7e5278a3c1d3f21b2b56e2639928f86815677a7126b093b2fdd08\"}}}]" : "[{\"operationName\":\"FollowButton_UnfollowUser\",\"variables\":{\"input\":{\"targetID\":\"" + targetId + "\"}},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"f7dae976ebf41c755ae2d758546bfd176b4eeb856656098bb40e0a672ca0d880\"}}}]";
                this.delay = delayInt * 1000;
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
                        Bad = 0;
                        Good = 0;
                        tasks.Clear();
                        Dispatcher.Invoke(() =>
                        {
                            btn.Tag = "start";
                            btn.Content = "Start";
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

        }
        private void Button_Upload_Proxies(object sender, RoutedEventArgs e)
        {
            // HTTP or any: 188.130.143.204:5500@31254134:ProxySoxybot or 188.130.143.204:5500
            // Auto: http://188.130.143.204:5500@31254134:ProxySoxybot or http://188.130.143.204:5500
            if (tasks.Any()) return;

            List<(ProxyClient, string)> proxies = new();
            string proxyType = ProxyType.Text.ToLower();
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
                                        .Select(x => x.Trim().Replace("@", ":"))
                                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t') || proxyTypes.Any(y => x.StartsWith(y))) && x.Contains(':')))
                {
                    try
                    {
                        proxies.Add((ProxyClient.Parse($"{proxyType}://{line}"), $"{proxyType}:{line}"));
                    }
                    catch { }
                }
            else if (proxyType == "auto")
                foreach (var line in File.ReadAllLines(filepath)
                        .Select(x => x.Trim().Replace("@", ":"))
                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t')) && proxyTypes.Any(y => x.StartsWith(y)) && x.Contains(':')))
                {
                    try
                    {
                        ProxyClient proxy = ProxyClient.Parse(line);
                        proxies.Add((proxy, proxy.Type switch
                        {
                            Leaf.xNet.ProxyType.HTTP => "http",
                            Leaf.xNet.ProxyType.Socks5 => "socks5",
                            Leaf.xNet.ProxyType.Socks4 => "socks4",
                        } + $":{proxy.Host}:{proxy.Port}:{proxy.Username}:{proxy.Password}"));
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
        }
        public static void UnSafeStop() => tasks.ForEach(x => x.Interrupt());
    }
}
