using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TwitchPIMP
{
    internal class Configuration
    {
        internal class Chatbot
        {
            internal (int, int) thread_start_delay_interval;
            internal (int, int) delay_interval_after_advanced_messages;
            internal (int, int) advanced_messages_cooldown_interval;
            internal (int, int) amount_spam_interval;
            internal (int, int) delay_interval_after_spam_messages;
        }
        internal class Viewersbot
        {
            internal (int, int) thread_start_delay_interval;
        }

        private static readonly JsonSerializerOptions optionsSerializer = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        internal static readonly string ip = "176.119.156.39";
        internal static readonly int port = 8888;
        internal static readonly string version = "23.04.2023";
        internal static readonly string[] userAgents =
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/109.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; x64; rv:109.0) Gecko/20100101 Firefox/110.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36"
        };
        internal static Chatbot chatbot;
        internal static Viewersbot viewersbot;
        internal static ConfigurationJson.Authorization authorization;
        internal static ConfigurationJson.Other other;

        internal static void Parse()
        {
            var deserializedJson = JsonSerializer.Deserialize<ConfigurationJson>(File.ReadAllText("configuration.json"));
            chatbot = new()
            {
                thread_start_delay_interval = (int.Parse(deserializedJson.chatbot.thread_start_delay_interval.Split('-')[0]) * 1000, int.Parse(deserializedJson.chatbot.thread_start_delay_interval.Split('-')[1]) * 1000),
                delay_interval_after_advanced_messages = (int.Parse(deserializedJson.chatbot.delay_interval_after_advanced_messages.Split('-')[0]) * 1000, int.Parse(deserializedJson.chatbot.delay_interval_after_advanced_messages.Split('-')[1]) * 1000),
                advanced_messages_cooldown_interval = (int.Parse(deserializedJson.chatbot.advanced_messages_cooldown_interval.Split('-')[0]), int.Parse(deserializedJson.chatbot.advanced_messages_cooldown_interval.Split('-')[1])),
                amount_spam_interval = (int.Parse(deserializedJson.chatbot.amount_spam_interval.Split('-')[0]), int.Parse(deserializedJson.chatbot.amount_spam_interval.Split('-')[1])),
                delay_interval_after_spam_messages = (int.Parse(deserializedJson.chatbot.delay_interval_after_spam_messages.Split('-')[0]) * 1000, int.Parse(deserializedJson.chatbot.delay_interval_after_spam_messages.Split('-')[1]) * 1000),
            };
            authorization = deserializedJson.authorization;
            other = deserializedJson.other;
            viewersbot = new()
            {
                thread_start_delay_interval = (int.Parse(deserializedJson.viewersbot.thread_start_delay_interval.Split('-')[0]) * 1000, int.Parse(deserializedJson.viewersbot.thread_start_delay_interval.Split('-')[1]) * 1000),
            };
        }
        internal static void Save()
        {
            var config = JsonSerializer.Serialize(new ConfigurationJson()
            {
                chatbot = new()
                {
                    thread_start_delay_interval = $"{chatbot.thread_start_delay_interval.Item1 / 1000}-{chatbot.thread_start_delay_interval.Item2 / 1000}",
                    delay_interval_after_advanced_messages = $"{chatbot.delay_interval_after_advanced_messages.Item1 / 1000}-{chatbot.delay_interval_after_advanced_messages.Item2 / 1000}",
                    advanced_messages_cooldown_interval = $"{chatbot.advanced_messages_cooldown_interval.Item1}-{chatbot.advanced_messages_cooldown_interval.Item2}",
                    amount_spam_interval = $"{chatbot.amount_spam_interval.Item1}-{chatbot.amount_spam_interval.Item2}",
                    delay_interval_after_spam_messages = $"{chatbot.delay_interval_after_spam_messages.Item1 / 1000}-{chatbot.delay_interval_after_spam_messages.Item2 / 1000}",
                },
                viewersbot = new()
                {
                    thread_start_delay_interval = $"{viewersbot.thread_start_delay_interval.Item1 / 1000}-{viewersbot.thread_start_delay_interval.Item2 / 1000}",
                },
                authorization = authorization,
                other = other
            }, optionsSerializer);
            File.WriteAllText("configuration.json", config);
        }
    }
}
