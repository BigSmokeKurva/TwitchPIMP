using Leaf.xNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для AutoregPage.xaml
    /// </summary>
    public partial class AutoregPage : Page, INotifyPropertyChanged
    {
        private static readonly Random rnd = new();
        private static readonly string[] proxyTypes = new[] { "http", "socks4", "socks5" };
        private static readonly char[] vowels = "aeuoyi".ToCharArray();
        private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly char[] charsPassword = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@!_-=@!_-=".ToCharArray();
        private static readonly char[] consonants = "qwrtpsdfghjklzxcvbnmqwrtpsdfghjklzxcvbnmQWRTPSDFGHJKLZXCVBNM_".ToCharArray();
        private static readonly string[] userAgents =
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/109.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; x64; rv:109.0) Gecko/20100101 Firefox/110.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36"
        };
        private static readonly Regex IntegrityTokenRegex = new("token\":\"(.*?)\"");
        private static readonly Regex accessTokenRegex = new("token\":\"(.*?)\"");
        private static readonly Regex userIdRegex = new("rID\":\"(.*?)\"");
        private static readonly List<Thread> tasks = new();
        private static readonly string clientVersion = "3040e141-5964-4d72-b67d-e73c1cf355b";
        private static readonly string clientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
        private string capsolverApi;
        private bool confirmEmail;
        private int _good = 0;
        private int _bad = 0;
        private (ProxyClient, string)[] _proxies = Array.Empty<(ProxyClient, string)>();
        private List<string> result = new();
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

        #region Обработчик изменений переменных
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        #endregion

        public AutoregPage()
        {
            DataContext = this;
            InitializeComponent();
            CapsolverApi.Text = Configuration.other.capsolver_api;
        }
        private void ThreadBot()
        {
            string res;
            HttpResponse _res;
            string deviceId;
            string integrityToken;
            string userAgent;
            string userId;
            string code;
            string nickname;
            string password;
            string accessToken;
            (ProxyClient, string) proxy;
            KasadaResponse kasada;
            Email email = new();
            using HttpRequest httpRequest = new();
            httpRequest.ConnectTimeout = 10000;
            httpRequest.KeepAliveTimeout = 10000;
            //httpRequest.IgnoreProtocolErrors = true;
            httpRequest["Accept"] = "*/*";
            httpRequest["Client-Version"] = clientVersion;
            httpRequest["Accept-Encoding"] = "deflate";
            httpRequest["Client-Id"] = clientId;
            try
            {
                while (true)
                {
                    //httpRequest.Cookies?.Clear();
                    //httpRequest.ClearAllHeaders();
                    userAgent = userAgents[rnd.Next(userAgents.Length)];
                    proxy = Proxies[rnd.Next(0, Proxies.Length)];
                    httpRequest["User-Agent"] = userAgent;
                    httpRequest.Proxy = proxy.Item1;
                    try
                    {
                        nickname = CreateNickname();
                        password = CreatePassword();
                        try
                        {
                            email.NewEmail(nickname, proxy.Item1);
                        }
                        catch (ThreadInterruptedException) { return; }
                        catch
                        {
                            Thread.Sleep(rnd.Next(100, 1000));
                            email.NewEmail(nickname, proxy.Item1);
                        }
                        _res = httpRequest.Get("https://twitch.tv");
                        deviceId = _res.Cookies.GetCookies("https://twitch.tv").First(x => x.Name == "unique_id").Value;
                        httpRequest["X-Device-Id"] = deviceId;
                        kasada = SolveKasada(proxy.Item2, userAgent);
                        httpRequest["x-kpsdk-ct"] = kasada.solution.xkpsdkct;
                        httpRequest["x-kpsdk-cd"] = kasada.solution.xkpsdkcd;
                        _res = httpRequest.Post("https://passport.twitch.tv/integrity");
                        integrityToken = IntegrityTokenRegex.Match(_res.ToString()).Groups[1].Value;
                        httpRequest.Cookies.Add(_res.Cookies.GetCookies("https://passport.twitch.tv"));
                        res = httpRequest.Post("https://passport.twitch.tv/protected_register",
                            "{\"username\": \"" + nickname + "\", \"password\": \"" + password + "\", \"email\": \"" + email.address + "\", \"birthday\": {\"day\": " + rnd.Next(1, 28) + ", \"month\": " + rnd.Next(1, 11) + ", \"year\": " + rnd.Next(1970, 2003) + "}, \"client_id\": \"" + clientId + "\", \"integrity_token\": \"" + integrityToken + "\"}",
                            "application/json").ToString();
                        accessToken = accessTokenRegex.Match(res).Groups[1].Value;
                        userId = userIdRegex.Match(res).Groups[1].Value;
                        // Почта
                        if (confirmEmail)
                        {
                            code = email.GetCode();
                            kasada = SolveKasada(proxy.Item2, userAgent);
                            httpRequest["x-kpsdk-ct"] = kasada.solution.xkpsdkct;
                            httpRequest["x-kpsdk-cd"] = kasada.solution.xkpsdkcd;
                            httpRequest["Authorization"] = $"OAuth {accessToken}";
                            res = httpRequest.Post("https://gql.twitch.tv/integrity").ToString();
                            integrityToken = IntegrityTokenRegex.Match(res).Groups[1].Value;
                            httpRequest["Client-Integrity"] = integrityToken;
                            res = httpRequest.Post("https://gql.twitch.tv/gql",
                                "[{\"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"05eba55c37ee4eff4dae260850dd6703d99cfde8b8ec99bc97a67e584ae9ec31\"}}, \"operationName\": \"ValidateVerificationCode\", \"variables\": {\"input\": {\"code\": \"" + code + "\", \"key\": \"" + userId + "\", \"address\": \"" + email.address + "\"}}}]",
                                "application/json").ToString();
                        }
                        result.Add(accessToken);
                        Good++;
                    }
                    catch (ThreadInterruptedException) { return; }
                    catch { Bad++; }
                }
            }
            catch (ThreadInterruptedException) { return; }
            catch { Bad++; }
        }

        private static string CreateNickname()
        {
            int length = rnd.Next(7, 15);
            StringBuilder nickname = new(length);
            bool firstVowel;
            while (nickname.Length < length)
            {
                firstVowel = rnd.Next(0, 2) == 0;
                if (firstVowel)
                {
                    nickname.Append(vowels[rnd.Next(0, vowels.Length)]);
                    nickname.Append(consonants[rnd.Next(0, consonants.Length)]);
                    continue;
                }
                nickname.Append(consonants[rnd.Next(0, consonants.Length)]);
                nickname.Append(vowels[rnd.Next(0, vowels.Length)]);
            }
            if (rnd.Next(0, 2) == 0)
            {
                if (rnd.Next(0, 2) == 0)
                    nickname.Append('_');
                nickname.Append(rnd.Next(0, 10000));
            }
            if (nickname.Length > length)
                nickname = nickname.Remove(0, nickname.Length - length);
            return nickname.ToString();
        }
        private static string CreatePassword()
        {
            int length = rnd.Next(8, 30);
            StringBuilder nickname = new(length);
            while (nickname.Length < length)
                nickname.Append(charsPassword[rnd.Next(0, charsPassword.Length)]);
            return nickname.ToString();
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
            // TODO
            Button btn = (Button)sender;
            string threads;
            string capsolverApi;
            Thread thread;
            if ((string)btn.Tag == "start")
            {
                threads = Threads.Text.Trim();
                capsolverApi = CapsolverApi.Text.Trim();
                if (string.IsNullOrEmpty(capsolverApi) || string.IsNullOrWhiteSpace(capsolverApi) || capsolverApi.Contains('\n') || capsolverApi.Contains('\t') || capsolverApi.Contains("  ") ||
                    string.IsNullOrEmpty(threads) || string.IsNullOrWhiteSpace(threads) || threads.Contains('\n') || threads.Contains('\t') || threads.Contains("  ") || !int.TryParse(threads, out int threadsInt) ||
                    !Proxies.Any())
                    return;
                this.capsolverApi = capsolverApi;
                this.confirmEmail = (bool)ConfirmEmail.IsChecked;
                for (int i = 0; i < threadsInt; i++)
                {
                    thread = new(ThreadBot);
                    thread.Start();
                    tasks.Add(thread);
                }
                btn.Tag = "stop";
                btn.Content = "Stop";
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
                        btn.Tag = "save";
                        btn.Content = "Save";
                        //StartBtn.Content = "Start";
                    });
                }).Start();
            }
            else if ((string)btn.Tag == "save")
            {
                try
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
                    File.AppendAllLines(filepath, this.result);
                    this.result.Clear();
                    btn.Tag = "start";
                    btn.Content = "Start";
                }
                catch { }
            }
        }
        private void Button_Upload_Proxies(object sender, RoutedEventArgs e)
        {
            // HTTP or any: 188.130.143.204:5500@31254134:ProxySoxybot or 188.130.143.204:5500
            // Auto: http://188.130.143.204:5500@31254134:ProxySoxybot or http://188.130.143.204:5500
            if (tasks.Any()) return;

            List<(ProxyClient, string)> proxies = new();
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
                                        .Select(x => x.Trim().Replace("@", ":"))
                                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t') || proxyTypes.Any(y => x.StartsWith(y)))))
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
                        .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Contains('\t')) && proxyTypes.Any(y => x.StartsWith(y))))
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
        public static void UnSafeStop() => tasks.ForEach(x => x.Interrupt());

    }
}
