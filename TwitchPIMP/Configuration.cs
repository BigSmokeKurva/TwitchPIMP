using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TwitchPIMP
{
    class Configuration
    {
        public class ConfigurationJson
        {
            public class Authorization
            {
                [JsonPropertyName("key")]
                public string key { get; set; }
            }
            public class Chatbot
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


            [JsonPropertyName("authorization")]
            public Authorization authorization { get; set; }

            [JsonPropertyName("chatbot")]
            public Chatbot chatbot { get; set; }
        }
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
                authorization = authorization
            }, optionsSerializer);
            File.WriteAllText("configuration.json", config);
        }
    }
}
