using Leaf.xNet;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace TwitchPIMP;

public struct KasadaResponse
{
    public struct Solution
    {
        [JsonPropertyName("user-agent")]
        public string useragent { get; set; }

        [JsonPropertyName("x-kpsdk-cd")]
        public string xkpsdkcd { get; set; }

        [JsonPropertyName("x-kpsdk-ct")]
        public string xkpsdkct { get; set; }
    }

    [JsonPropertyName("errorId")]
    public int errorId { get; set; }

    [JsonPropertyName("success")]
    public bool success { get; set; }

    [JsonPropertyName("status")]
    public string status { get; set; }

    [JsonPropertyName("solution")]
    public Solution solution { get; set; }

    [JsonPropertyName("type")]
    public string type { get; set; }
}
internal class Email
{
    private static readonly Regex domainRegex = new("domain\":\"(.*?)\"");
    private static readonly Regex addressRegex = new("address\":\"(.*?)\"");
    private static readonly Regex emailTokenRegex = new("token\":\"(.*?)\"");
    private static readonly Regex codeRegex = new("\"subject\":\"(.*?) ");
    private static readonly Random rnd = new();
    private static string[] domains = Array.Empty<string>();
    internal string address;
    internal string password;
    internal string token;
    private ProxyClient proxyClient;

    internal static void GetDomains()
    {
        using HttpRequest httpRequest = new();
        string res = httpRequest.Get("https://api.mail.tm/domains").ToString();
        domains = domainRegex.Matches(res).Select(domain => domain.Groups[1].Value).ToArray();
    }
    internal void NewEmail(string nickname, ProxyClient proxy)
    {
        proxyClient = proxy;
        string res;
        this.password = $"TwitchPIMP_{rnd.Next(1000, 10000000)}!";
        string postData = "{\"address\": \"" + nickname.Replace("_", string.Empty).ToLower() + "@" + domains[rnd.Next(0, domains.Length)] + "\", \"password\": \"" + password + "\"}";
        using HttpRequest httpRequest = new();
        httpRequest.Proxy = proxyClient;
        httpRequest.AllowAutoRedirect = false;
        httpRequest["Accept-Encoding"] = "deflate";
        res = httpRequest.Post("https://api.mail.tm/accounts",
            postData
            , "application/json").ToString();
        this.address = addressRegex.Match(res).Groups[1].Value;
        res = httpRequest.Post("https://api.mail.tm/token",
            postData
            , "application/json").ToString();
        this.token = emailTokenRegex.Match(res).Groups[1].Value;
    }
    internal string GetCode()
    {
        string res;
        MatchCollection match;
        using HttpRequest httpRequest = new();
        httpRequest["Authorization"] = $"Bearer {token}";
        httpRequest.AllowAutoRedirect = false;
        httpRequest["Accept-Encoding"] = "deflate";
        httpRequest.Proxy = proxyClient;

        for (int i = 0; i < 30; i++)
        {
            Thread.Sleep(2000);
            res = httpRequest.Get("https://api.mail.tm/messages").ToString();
            match = codeRegex.Matches(res);
            if (match.Any())
                return match[0].Groups[1].Value;
        }
        throw new Exception();
    }
}
public struct ConfigurationJson
{
    public struct Authorization
    {
        [JsonPropertyName("key")]
        public string key { get; set; }
    }
    public struct Chatbot
    {
        [JsonPropertyName("thread_start_delay_interval")]
        public string thread_start_delay_interval { get; set; }

        [JsonPropertyName("delay_interval_after_advanced_messages")]
        public string delay_interval_after_advanced_messages { get; set; }

        [JsonPropertyName("advanced_messages_cooldown_interval")]
        public string advanced_messages_cooldown_interval { get; set; }

        [JsonPropertyName("amount_spam_interval")]
        public string amount_spam_interval { get; set; }

        [JsonPropertyName("delay_interval_after_spam_messages")]
        public string delay_interval_after_spam_messages { get; set; }
    }
    public struct Other
    {
        [JsonPropertyName("capsolver_api")]
        public string capsolver_api { get; set; }
    }
    public struct Viewersbot
    {
        [JsonPropertyName("thread_start_delay_interval")]
        public string thread_start_delay_interval { get; set; }
    }

    [JsonPropertyName("authorization")]
    public Authorization authorization { get; set; }

    [JsonPropertyName("chatbot")]
    public Chatbot chatbot { get; set; }

    [JsonPropertyName("other")]
    public Other other { get; set; }

    [JsonPropertyName("viewersbot")]
    public Viewersbot viewersbot { get; set; }
}
public struct AuthorizationResponse
{
    public struct ConfigSoftJson
    {
        [JsonPropertyName("version")]
        public string version { get; set; }

        [JsonPropertyName("invalid_version_msg")]
        public string invalid_version_msg { get; set; }

        [JsonPropertyName("hi_msg")]
        public string hi_msg { get; set; }
    }

    [JsonPropertyName("response")]
    public string response { get; set; }

    [JsonPropertyName("key")]
    public string key { get; set; }

    [JsonPropertyName("license_date")]
    public string license_date { get; set; }

    [JsonPropertyName("license_time_left")]
    public string license_time_left { get; set; }

    [JsonPropertyName("is_active")]
    public bool is_active { get; set; }

    [JsonPropertyName("config")]
    public ConfigSoftJson config { get; set; }
}

