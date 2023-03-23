using Leaf.xNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для ViewersBotPage.xaml
    /// </summary>
    public partial class ViewersBotPage : Page, INotifyPropertyChanged
    {
        private static string postData;
        private static readonly Regex tokenRegex = new("value\":\"(.*?)\",\"signature");
        private static readonly Regex sigRegex = new("signature\":\"(.*?)\"");
        private static readonly Random rnd = new();
        //private static readonly string[] os = { "Windows NT 6.0", "Windows NT 6.1", "Windows NT 6.2", "Windows NT 6.4", "Windows NT 10.0; Win64; x64", "Windows NT 10.0", "Windows NT 10.0; WOW64", "Windows NT 10.0; Win64; x64" };
        private static readonly string[] proxyTypes = new[] { "http", "socks4", "socks5" };
        private static readonly List<Thread> tasks = new();
        private string channel;
        private bool experimentalMode;
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

        public ViewersBotPage()
        {
            DataContext = this;
            InitializeComponent();
        }
        //private static string GetUserAgent()
        //{
        //    var webkit = rnd.Next(500, 537);
        //    var version = $"{rnd.Next(90, 110)}.0{rnd.Next(0, 1500)}.{rnd.Next(0, 1000)}";
        //    return $"Mozilla/5.0 ({os[rnd.Next(0, os.Length)]}) AppleWebKit/{webkit}.0 (KHTML, like Gecko) Chrome/{version} Safari/{webkit}";
        //}

        //private async Task ThreadOld()
        //{
        //    // Устарел
        //    string res;
        //    string token;
        //    string sig;
        //    string url;
        //    WebProxy proxy = new();
        //    WebProxy tempProxy;
        //    HttpClientHandler handler = new() { UseCookies = false, UseProxy = useProxy, Proxy = proxy };
        //    HttpClient client = new(handler)
        //    {
        //        Timeout = TimeSpan.FromSeconds(5)
        //    };
        //    client.DefaultRequestHeaders.Accept.Add(new("*/*"));
        //    client.DefaultRequestHeaders.AcceptLanguage.Add(new("en-US"));
        //    client.DefaultRequestHeaders.Referrer = new($"https://www.twitch.tv/{channel}");
        //    while (!cancelTokenSource.Token.IsCancellationRequested)
        //    {
        //        client.DefaultRequestHeaders.UserAgent.Clear();
        //        if (useProxy)
        //        {
        //            tempProxy = Proxies[rnd.Next(0, Proxies.Length)];
        //            proxy.Address = tempProxy.Address;
        //            proxy.Credentials = tempProxy.Credentials;
        //        }
        //        client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
        //        client.DefaultRequestHeaders.Authorization = new("OAuth", Tokens[rnd.Next(0, Tokens.Length)]);
        //        try
        //        {
        //            res = await (await client.PostAsJsonAsync("https://gql.twitch.tv/gql", postJson, cancelTokenSource.Token)).Content.ReadAsStringAsync();
        //            token = tokenRegex.Match(res).Groups[1].Value.Replace("\\", string.Empty);
        //            sig = sigRegex.Match(res).Groups[1].Value;
        //            res = await client.GetStringAsync($"https://usher.ttvnw.net/api/channel/hls/{channel}.m3u8?sig={sig}&token={token}", cancelTokenSource.Token);
        //            url = res[res.IndexOf("https://")..].Trim();
        //            await client.SendAsync(new(HttpMethod.Head, url), cancelTokenSource.Token);
        //            Good++;
        //        }
        //        catch (TaskCanceledException) { }
        //        catch
        //        {
        //            Bad++;
        //        }

        //    }
        //}
        //private async Task Thread1()
        //{
        //    string res;
        //    string token;
        //    string sig;
        //    string url;
        //    HttpWebRequest request;
        //    WebHeaderCollection headers;
        //    StreamWriter streamWriter;
        //    StreamReader streamReader;
        //    await Task.Delay(5);
        //    while (!cancelTokenSource.Token.IsCancellationRequested)
        //    {
        //        headers = new()
        //        {
        //            { "Accept", "*/*" },
        //            {"Accept-Language", "en-US" },
        //            {"Referer",  $"https://www.twitch.tv/{channel}"},
        //            {"User-Agent", GetUserAgent() },
        //            {"Authorization", $"OAuth {Tokens[rnd.Next(0, Tokens.Length)]}" },
        //            //{"content-type", "application/json; charset=UTF-8" },
        //            {"Sec-Fetch-Mode", "cors" },
        //            {"X-Requested-With", "XMLHttpRequest" }
        //        };

        //        try
        //        {
        //            request = HttpWebRequest.CreateHttp("https://gql.twitch.tv/gql");
        //            request.Method = "POST";
        //            request.Headers = headers;
        //            streamWriter = new StreamWriter(await request.GetRequestStreamAsync());
        //            await streamWriter.WriteAsync(postJson);
        //            streamWriter.Close();
        //            streamReader = new StreamReader((await request.GetResponseAsync()).GetResponseStream());
        //            res = await streamReader.ReadToEndAsync(cancelTokenSource.Token);
        //            token = tokenRegex.Match(res).Groups[1].Value.Replace("\\", string.Empty);
        //            sig = sigRegex.Match(res).Groups[1].Value;
        //            request = HttpWebRequest.CreateHttp($"https://usher.ttvnw.net/api/channel/hls/{channel}.m3u8?sig={sig}&token={token}");
        //            request.Headers = headers;
        //            request.Method = "GET";
        //            streamReader = new StreamReader((await request.GetResponseAsync()).GetResponseStream());
        //            res = await streamReader.ReadToEndAsync(cancelTokenSource.Token);
        //            url = res[res.IndexOf("https://")..].Trim();
        //            request = HttpWebRequest.CreateHttp(url);
        //            request.Headers = headers;
        //            request.Method = "HEAD";
        //            await request.GetResponseAsync();
        //            Good++;
        //        }
        //        catch (TaskCanceledException) { }
        //        catch
        //        {
        //            Bad++;
        //        }
        //    }
        //}
        //private void ThreadProxy1()
        //{
        //    string res;
        //    string token;
        //    string sig;
        //    string url;
        //    HttpWebRequest request;
        //    WebHeaderCollection headers;
        //    StreamWriter streamWriter;
        //    StreamReader streamReader;
        //    WebProxy proxy;
        //    while (!cancelTokenSource.Token.IsCancellationRequested)
        //    {
        //        headers = new()
        //        {
        //            { "Accept", "*/*" },
        //            {"Accept-Language", "en-US" },
        //            {"Referer",  $"https://www.twitch.tv/{channel}"},
        //            {"User-Agent", GetUserAgent() },
        //            {"Authorization", $"OAuth {Tokens[rnd.Next(0, Tokens.Length)]}" },
        //            //{"content-type", "application/json; charset=UTF-8" },
        //            {"Sec-Fetch-Mode", "cors" },
        //            {"X-Requested-With", "XMLHttpRequest" }
        //        };
        //        proxy = Proxies[rnd.Next(0, Proxies.Length)];
        //        try
        //        {
        //            request = HttpWebRequest.CreateHttp("https://gql.twitch.tv/gql");
        //            request.Timeout = 5000;
        //            request.Method = "POST";
        //            request.Proxy = proxy;
        //            request.Headers = headers;
        //            streamWriter = new StreamWriter(request.GetRequestStream());
        //            streamWriter.Write(postJson);
        //            streamWriter.Close();
        //            streamReader = new StreamReader((request.GetResponse()).GetResponseStream());
        //            res = streamReader.ReadToEnd();
        //            token = tokenRegex.Match(res).Groups[1].Value.Replace("\\", string.Empty);
        //            sig = sigRegex.Match(res).Groups[1].Value;
        //            request = HttpWebRequest.CreateHttp($"https://usher.ttvnw.net/api/channel/hls/{channel}.m3u8?sig={sig}&token={token}");
        //            request.Timeout = 5000;
        //            request.Headers = headers;
        //            request.Method = "GET";
        //            request.Proxy = proxy;
        //            streamReader = new StreamReader((request.GetResponse()).GetResponseStream());
        //            res = streamReader.ReadToEnd();
        //            url = res[res.IndexOf("https://")..].Trim();
        //            request = HttpWebRequest.CreateHttp(url);
        //            request.Timeout = 5000;
        //            request.Headers = headers;
        //            request.Method = "HEAD";
        //            request.Proxy = proxy;
        //            request.GetResponse();
        //            Good++;
        //        }
        //        catch (TaskCanceledException) { }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(   ex);
        //            Bad++;
        //        }
        //    }

        //}
        private void ThreadBot()
        {
            string res;
            string token;
            string sig;
            string url;
            string url2;
            using HttpRequest httpRequest = new();
            httpRequest.EnableEncodingContent = false;
            httpRequest.UseCookies = false;
            httpRequest.AddHeader(HttpHeader.Accept, "*/*");
            httpRequest.AddHeader(HttpHeader.AcceptLanguage, "en-us");
            httpRequest.AddHeader(HttpHeader.ContentType, "application/json; charset=UTF-8");
            httpRequest.AddHeader(HttpHeader.Referer, "https://www.twitch.tv/" + channel);
            httpRequest.AddHeader("Sec-Fetch-Mode", "cors");
            httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
            httpRequest.ConnectTimeout = 5000;
            httpRequest.KeepAliveTimeout = 5000;
            try
            {
                while (true)
                {
                    httpRequest["User-Agent"] = Configuration.userAgents[rnd.Next(Configuration.userAgents.Length)];
                    httpRequest["Authorization"] = $"OAuth {Tokens[rnd.Next(0, Tokens.Length)]}";
                    httpRequest.Proxy = useProxy ? Proxies[rnd.Next(0, Proxies.Length)] : null;
                    try
                    {
                        res = httpRequest.Post("https://gql.twitch.tv/gql", postData, "application/json").ToString();
                        token = tokenRegex.Match(res).Groups[1].Value.Replace("\\", string.Empty);
                        sig = sigRegex.Match(res).Groups[1].Value;
                        res = httpRequest.Get($"https://usher.ttvnw.net/api/channel/hls/{channel}.m3u8?sig={sig}&token={token}").ToString();
                        url = res[res.IndexOf("https://")..].Trim();
                        if (!experimentalMode)
                        {
                            httpRequest.Raw(Leaf.xNet.HttpMethod.HEAD, url);
                            Good++;
                        }
                        else
                        {
                            while (true)
                            {
                                res = httpRequest.Raw(Leaf.xNet.HttpMethod.GET, url).ToString();
                                url2 = res.Split('\n')[^2];
                                Thread.Sleep(4000);
                                res = httpRequest.Raw(Leaf.xNet.HttpMethod.HEAD, url2).ToString();
                                Good++;
                            }
                        }
                    }
                    catch (ThreadInterruptedException) { return; }
                    catch
                    {
                        Bad++;
                    }
                }
            }
            catch (ThreadInterruptedException) { return; }
        }
        private void Button_Start(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string nickname;
            string threads;
            Thread thread;
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
                experimentalMode = Mode.SelectedIndex == 1;
                postData = "{\"operationName\":\"PlaybackAccessToken_Template\",\"query\":\"query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: \\u0022web\\u0022, playerBackend: \\u0022mediaplayer\\u0022, playerType: $playerType}) @include(if: $isLive) {    value    signature    __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: \\u0022web\\u0022, playerBackend: \\u0022mediaplayer\\u0022, playerType: $playerType}) @include(if: $isVod) {    value    signature    __typename  }}\",\"variables\":{\"isLive\":true,\"login\":\"" + channel + "\",\"isVod\":false,\"vodID\":\"\",\"playerType\":\"site\"}}";

                for (int i = 0; i < threadsInt; i++)
                {
                    thread = new(ThreadBot);
                    thread.Start();
                    tasks.Add(thread);
                }
                btn.Tag = "stop";
                btn.Content = "Stop";
                //StartBtn.Content = "Stop";
            }
            else if ((string)btn.Tag == "stop")
            {
                btn.Tag = "stoping";
                new Thread(() =>
                {
                    tasks.ForEach(x => x.Interrupt());
                    tasks.ForEach(x => x.Join());
                    Bad = 0;
                    Good = 0;
                    tasks.Clear();
                    Dispatcher.Invoke(() =>
                    {
                        btn.Tag = "start";
                        btn.Content = "Start";
                        //StartBtn.Content = "Start";
                    });
                }).Start();
            }

        }
        private void Button_Upload_Proxies(object sender, RoutedEventArgs e)
        {
            // HTTP or any: 188.130.143.204:5500@31254134:ProxySoxybot or 188.130.143.204:5500
            // Auto: http://188.130.143.204:5500@31254134:ProxySoxybot or http://188.130.143.204:5500
            if (tasks.Any()) return;

            List<ProxyClient> proxies = new();
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
                                        .Select(x => x.Trim().Replace('@', ':'))
                                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t') || proxyTypes.Any(y => x.StartsWith(y))) && x.Contains(':')))
                {
                    try
                    {
                        proxies.Add(ProxyClient.Parse($"{proxyType}://{line}"));
                    }
                    catch { }
                }
            else if (proxyType == "auto")
                foreach (var line in File.ReadAllLines(filepath)
                        .Select(x => x.Trim().Replace('@', ':'))
                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t')) && proxyTypes.Any(y => x.StartsWith(y)) && x.Contains(':')))
                {
                    try
                    {
                        proxies.Add(ProxyClient.Parse(line));
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
