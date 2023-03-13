using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TwitchPIMP
{
    class Configuration
    {
        public class Chatbot
        {
            public (int, int) thread_start_delay_interval;
            public (int, int) delay_interval_after_advanced_messages;
            public (int, int) advanced_messages_cooldown_interval;
            public (int, int) amount_spam_interval;
            public (int, int) delay_interval_after_spam_messages;
        }
        private static readonly JsonSerializerOptions optionsSerializer = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        public static readonly string ip = "127.0.0.1";
        public static readonly int port = 8888;
        public static readonly string version = "1.6b";
        public static Chatbot chatbot;
        public static ConfigurationJson.Authorization authorization;
        public static ConfigurationJson.Other other;

        public static void Parse()
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
        }
        public static void Save()
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
                authorization = authorization,
                other = other
            }, optionsSerializer);
            File.WriteAllText("configuration.json", config);
        }
    }
}
