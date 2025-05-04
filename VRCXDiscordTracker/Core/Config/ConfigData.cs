using System.Text.Json.Serialization;

namespace VRCXDiscordTracker.Core.Config;
/// <summary>
/// 設定データを格納するクラス
/// </summary>
/// <remarks>JSON形式でシリアライズされる</remarks>
internal class ConfigData

{
    /// <summary>
    /// VRCXのデータベースファイルのパス
    /// </summary>
    [JsonPropertyName("databasePath")]
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>
    /// DiscordのWebhook URL
    /// </summary>
    [JsonPropertyName("discordWebhookUrl")]
    public string DiscordWebhookUrl { get; set; } = string.Empty;
}