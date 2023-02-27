using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для ViewersBotPage.xaml
    /// </summary>
    public partial class ViewersBotPage : Page, INotifyPropertyChanged
    {
        private class GqlTwitchPostJson
        {
            public class Variables
            {
                [JsonPropertyName("isLive")]
                public bool isLive { get; set; }

                [JsonPropertyName("login")]
                public string login { get; set; }

                [JsonPropertyName("isVod")]
                public bool isVod { get; set; }

                [JsonPropertyName("vodID")]
                public string vodID { get; set; }

                [JsonPropertyName("playerType")]
                public string playerType { get; set; }
            }

            [JsonPropertyName("operationName")]
            public string operationName { get; set; }

            [JsonPropertyName("query")]
            public string query { get; set; }

            [JsonPropertyName("variables")]
            public Variables variables { get; set; }
        }
        private static GqlTwitchPostJson postJson;
        private static CancellationTokenSource cancelTokenSource;
        private static readonly Regex tokenRegex = new("value\":\"(.*?)\",\"signature");
        private static readonly Regex sigRegex = new("signature\":\"(.*?)\"");
        private static readonly Random rnd = new();
        private static readonly string[] os = { "Windows NT 6.0", "Windows NT 6.1", "Windows NT 6.2", "Windows NT 6.4", "Windows NT 10.0; Win64; x64", "Windows NT 10.0", "Windows NT 10.0; WOW64", "Windows NT 10.0; Win64; x64" };
        private static readonly string[] proxyTypes = new[] { "http", "socks4", "socks5" };
        private static readonly List<Task> tasks = new();
        private string channel;
        private bool useProxy;
        private int threads;
        private int _good = 0;
        private int _bad = 0;
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

        public ViewersBotPage()
        {
            DataContext = this;
            InitializeComponent();
        }
        private static string GetUserAgent()
        {
            var webkit = rnd.Next(500, 537);
            var version = $"{rnd.Next(90, 110)}.0{rnd.Next(0, 1500)}.{rnd.Next(0, 1000)}";
            return $"Mozilla/5.0 ({os[rnd.Next(0, os.Length)]}) AppleWebKit/{webkit}.0 (KHTML, like Gecko) Chrome/{version} Safari/{webkit}";
        }

        private async Task Thread()
        {
            string res;
            string token;
            string sig;
            string url;
            WebProxy proxy = new();
            WebProxy tempProxy;
            HttpClientHandler handler = new() { UseCookies = false, UseProxy = useProxy, Proxy = proxy };
            HttpClient client = new(handler);
            client.DefaultRequestHeaders.Accept.Add(new("*/*"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new("en-US"));
            client.DefaultRequestHeaders.Referrer = new($"https://www.twitch.tv/{channel}");
            while (!cancelTokenSource.Token.IsCancellationRequested)
            {
                client.DefaultRequestHeaders.UserAgent.Clear();
                if (useProxy)
                {
                    tempProxy = Proxies[rnd.Next(0, Proxies.Length)];
                    proxy.Address = tempProxy.Address;
                    proxy.Credentials = tempProxy.Credentials;
                }
                client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
                client.DefaultRequestHeaders.Authorization = new("OAuth", Tokens[rnd.Next(0, Tokens.Length)]);
                try
                {
                    res = await (await client.PostAsJsonAsync("https://gql.twitch.tv/gql", postJson, cancelTokenSource.Token)).Content.ReadAsStringAsync();
                    //await Console.Out.WriteLineAsync(res);
                    token = tokenRegex.Match(res).Groups[1].Value.Replace("\\", string.Empty);
                    sig = sigRegex.Match(res).Groups[1].Value;
                    res = await client.GetStringAsync($"https://usher.ttvnw.net/api/channel/hls/{channel}.m3u8?sig={sig}&token={token}", cancelTokenSource.Token);
                    url = res[res.IndexOf("https://")..].Trim();
                    await Console.Out.WriteLineAsync(url);
                    await client.SendAsync(new(HttpMethod.Head, url), cancelTokenSource.Token);
                    Good++;
                }
                catch (TaskCanceledException) { }
                catch
                {
                    Bad++;
                }

            }
        }
        private void Button_Start(object sender, System.Windows.RoutedEventArgs e)
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
                    (useProxy && Proxies.Length == 0) || Tokens.Length == 0)
                {
                    return;
                }
                cancelTokenSource = new();
                this.channel = nickname;
                this.threads = threadsInt;
                postJson = new GqlTwitchPostJson()
                {
                    operationName = "PlaybackAccessToken_Template",
                    query = "query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: \"web\", playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isLive) {    value    signature    __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: \"web\", playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isVod) {    value    signature    __typename  }}",
                    variables = new()
                {
                    isLive = true,
                    login = channel,
                    isVod = false,
                    vodID = string.Empty,
                    playerType = "site"
                }
                };
                for(int i =0; i<threadsInt; i++)
                {
                    tasks.Add(this.Thread());
                }
                Task.WhenAll(tasks);
                btn.Tag = "stop";
                StartBtn.Content = "Stop";
            }
            else if((string)btn.Tag == "stop")
            {
                btn.Tag = "stoping";
                cancelTokenSource.Cancel();
                new Thread(() =>
                {
                    Task.WaitAll(tasks.ToArray());
                    Dispatcher.Invoke(() => { btn.Tag = "start"; StartBtn.Content = "Start"; });
                }).Start();
                tasks.Clear();
            }

        }
        private void Button_Upload_Proxies(object sender, System.Windows.RoutedEventArgs e)
        {
            // HTTP or any: 188.130.143.204:5500@31254134:ProxySoxybot or 188.130.143.204:5500
            // Auto: http://188.130.143.204:5500@31254134:ProxySoxybot or http://188.130.143.204:5500

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
            if(proxyType != "auto")
                foreach(var line in File.ReadAllLines(filepath)
                                        .Select(x => x.Trim())
                                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t') || proxyTypes.Any(y => x.StartsWith(y)))))
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
                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t')) && proxyTypes.Any(y => x.StartsWith(y))))
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
        private void Button_Upload_Tokens(object sender, System.Windows.RoutedEventArgs e)
        {
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

    }
}
