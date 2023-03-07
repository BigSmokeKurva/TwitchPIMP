using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

        private static readonly JsonSerializerOptions optionsSerializer = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        public static readonly string ip = "127.0.0.1";
        public static readonly int port = 8888;
        public static readonly string version = "1.6b";
        public static ConfigurationJson.Chatbot chatbot;
        public static ConfigurationJson.Authorization authorization;

        public static void Parse()
        {
            var deserializedJson = JsonSerializer.Deserialize<ConfigurationJson>(File.ReadAllText("configuration.json"));
            chatbot = deserializedJson.chatbot;
            authorization = deserializedJson.authorization;
        }
        public static void Save()
        {
            var config = JsonSerializer.Serialize(new ConfigurationJson()
            {
                chatbot = chatbot,
                authorization = authorization
            }, optionsSerializer);
            File.WriteAllText("configuration.json", config);
        }
    }
}
