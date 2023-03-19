using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Puppeteer = PuppeteerSharp;

namespace TwitchPIMP
{
    /// <summary>
    /// Логика взаимодействия для WarmupPage.xaml
    /// </summary>
    public partial class WarmupPage : Page, INotifyPropertyChanged
    {
        private static readonly List<Task> tasks = new();
        private static readonly Puppeteer.BrowserFetcher browserFetcher = new();
        private static Puppeteer.IBrowser browser = null;
        private static readonly Puppeteer.LaunchOptions launchOptions = new()
        {
            Headless = true,
            Args = new string[] {
                                    //"--incognito",
                                    "--disable-blink-features=AutomationControlled",
                                    "--blink-settings=imagesEnabled=false"
                                },
        };
        private Puppeteer.CookieParam[] _cookies = Array.Empty<Puppeteer.CookieParam>();
        private int _good = 0;
        private int _bad = 0;
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
        public Puppeteer.CookieParam[] Cookies
        {
            get => _cookies;
            set
            {
                _cookies = value;
                OnPropertyChanged();
            }
        }
        #region Обработчик изменений переменных
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        #endregion

        public WarmupPage()
        {
            DataContext = this;
            InitializeComponent();
        }
        private async Task ThreadBot(Puppeteer.CookieParam[] cookies)
        {
            Puppeteer.IBrowserContext browserContext = await browser.CreateIncognitoBrowserContextAsync();
            await using Puppeteer.IPage page = await browserContext.NewPageAsync();
            foreach (var cookie in cookies)
            {
                try
                {
                    await page.SetCookieAsync(cookie);
                    await page.GoToAsync("https://www.twitch.tv/", waitUntil: Puppeteer.WaitUntilNavigation.Networkidle2);
                    await page.Client.SendAsync("Network.clearBrowserCookies");
                    await page.Client.SendAsync("Network.clearBrowserCache");
                    Good++;
                }
                catch (Puppeteer.TargetClosedException)
                {
                    return;
                }
                catch (Puppeteer.PuppeteerException)
                {
                    return;
                }
                catch
                {
                    // TODO закрыть страницу BAD
                    Bad++;
                }
            }
        }
        private async void Button_Start(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string threads;
            if ((string)btn.Tag == "start")
            {
                threads = Threads.Text.Trim();
                if (string.IsNullOrEmpty(threads) || string.IsNullOrWhiteSpace(threads) || threads.Contains('\n') || threads.Contains('\t') || threads.Contains("  ") || !int.TryParse(threads, out int threadsInt) ||
                    !Cookies.Any() || threadsInt == 0)
                    return;
                await browserFetcher.DownloadAsync();
                browser = await Puppeteer.Puppeteer.LaunchAsync(launchOptions);
                foreach (var batch in Cookies.Batch(Cookies.Length / threadsInt))
                    tasks.Add(ThreadBot((Puppeteer.CookieParam[])batch));

                new Thread(async () =>
                {
                    try
                    {
                        await Task.WhenAll(tasks);

                        Bad = 0;
                        Good = 0;
                        if (!browser.IsClosed)
                            await browser.CloseAsync();
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
                await browser.CloseAsync();
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
            Cookies = File.ReadAllLines(filepath)
                        .Select(x => x.Trim())
                        .Where(x =>
                                        !(string.IsNullOrEmpty(x)
                                        || string.IsNullOrWhiteSpace(x)
                                        || x.Contains('\t')
                                        || x.Contains(' '))).ToArray()
                        .Select(x => new Puppeteer.CookieParam()
                        {
                            Domain = ".twitch.tv",
                            Name = "auth-token",
                            Value = x,
                            Path = "/",
                        }).ToArray();
        }
        public static void UnSafeStop()
        {
            if(browser is not null && !browser.IsClosed)
                browser.CloseAsync().Wait();
        }

    }
}
