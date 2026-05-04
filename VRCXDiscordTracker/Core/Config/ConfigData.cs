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

    /// <summary>
    /// アプリケーション起動時に通知を送信するかどうか
    /// </summary>
    [JsonPropertyName("notifyOnStart")]
    public bool NotifyOnStart { get; set; } = true;

    /// <summary>
    /// アプリケーション終了時に通知を送信するかどうか
    /// </summary>
    [JsonPropertyName("notifyOnExit")]
    public bool NotifyOnExit { get; set; } = true;

    /// <summary>
    /// 通知対象とするロケーションの数
    /// </summary>
    [JsonPropertyName("locationCount")]
    public int LocationCount { get; set; } = 15;
}
