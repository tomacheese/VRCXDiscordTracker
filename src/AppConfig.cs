using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VRCXDiscordTracker
{
    /// <summary>
    /// アプリケーションの設定を管理するクラス
    /// </summary>
    internal class AppConfig
    {
        /// <summary>
        /// 設定ファイルのパス
        /// </summary>
        /// <remarks>アプリケーションの実行ディレクトリに config.json という名前で保存される</remarks>
        /// <example>config.json</example>
        private static readonly string ConfigFilePath = Path.Combine(Environment.CurrentDirectory, "config.json");

        /// <summary>
        /// 設定データを格納するクラス
        /// </summary>
        /// <remarks>JSON形式でシリアライズされる</remarks>
        private class ConfigData
        {
            /// <summary>
            /// VRCXのデータベースファイルのパス
            /// </summary>
            [JsonPropertyName("databasePath")]
            public string DatabasePath { get; set; }

            /// <summary>
            /// DiscordのWebhook URL
            /// </summary>
            [JsonPropertyName("discordWebhookUrl")]
            public string DiscordWebhookUrl { get; set; }
        }

        private static ConfigData _config = new ConfigData();

        static AppConfig()
        {
            Load();
        }

        /// <summary>
        /// 設定ファイルを読み込む
        /// </summary>
        private static void Load()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return;
            }

            var json = File.ReadAllText(ConfigFilePath);
            _config = JsonSerializer.Deserialize<ConfigData>(json);
        }

        /// <summary>
        /// 設定ファイルを保存する
        /// </summary>
        private static void Save()
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigFilePath, json);
        }

        /// <summary>
        /// VRChatのログディレクトリのパスを取得または設定するプロパティ
        /// </summary>
        /// <remarks>nullを設定すると、デフォルトのログディレクトリ（%LOCALAPPDATA%\..\LocalLow\VRChat\VRChat）が使用される</remarks>
        public static string DatabasePath
        {
            get
            {
                Load();
                return _config.DatabasePath;
            }
            set
            {
                // required exists path
                if (!File.Exists(value))
                {
                    throw new FileNotFoundException($"The specified file does not exist: {value}");
                }
                _config.DatabasePath = value?.Trim();
                Save();
            }
        }

        /// <summary>
        /// DiscordのWebhook URLを取得または設定するプロパティ
        /// </summary>
        /// <remarks>nullを設定すると、Discordへの通知は行われない</remarks>
        public static string DiscordWebhookUrl
        {
            get
            {
                Load();
                return _config.DiscordWebhookUrl;
            }
            set
            {
                // required http or https
                var trimmedValue = value?.Trim();
                if (trimmedValue != null && !trimmedValue.StartsWith("http://") && !trimmedValue.StartsWith("https://"))
                {
                    throw new ArgumentException("DiscordWebhookUrl must start with http or https.");
                }
                _config.DiscordWebhookUrl = trimmedValue;
                Save();
            }
        }
    }
}
