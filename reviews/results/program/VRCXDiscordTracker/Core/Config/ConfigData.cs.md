# ConfigData.cs レビュー

## 概要

`ConfigData.cs`は設定データを格納するクラスで、JSONシリアライズ・デシリアライズのためのモデルクラスとして機能しています。各設定項目はプロパティとして定義され、JSONシリアライズのためのアノテーションが付与されています。

## 良い点

1. クラスの責務が明確で、単一責任の原則を守っている
2. 各プロパティに適切なXMLドキュメントコメントが記述されている
3. `JsonPropertyName`アノテーションを使用して、JSONでのプロパティ名を明示している
4. デフォルト値を適切に設定している

## 改善点

### 1. バリデーション属性の追加

現在、プロパティの検証はAppConfigクラス内で行われていますが、モデル自体にもデータアノテーションを使用して検証を追加することで、検証ロジックを集約できます。

```csharp
using System.ComponentModel.DataAnnotations;
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
    [RegularExpression(@"^(https?:\/\/)?(discord\.com\/api\/webhooks\/[\w-]+\/[\w-]+)$", 
        ErrorMessage = "Must be a valid Discord webhook URL")]
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
    [Range(1, 100, ErrorMessage = "LocationCount must be between 1 and 100")]
    public int LocationCount { get; set; } = 5;
    
    /// <summary>
    /// 設定が有効かどうかを検証する
    /// </summary>
    public bool Validate(out ICollection<ValidationResult> results)
    {
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(this, 
            new ValidationContext(this), 
            results, 
            validateAllProperties: true);
    }
}
```

### 2. インターフェースの導入

将来的な拡張性を考慮して、インターフェースを導入することを検討すべきです。

```csharp
/// <summary>
/// 設定データのインターフェース
/// </summary>
internal interface IConfigData
{
    string DatabasePath { get; set; }
    string DiscordWebhookUrl { get; set; }
    bool NotifyOnStart { get; set; }
    bool NotifyOnExit { get; set; }
    int LocationCount { get; set; }
    
    bool Validate(out ICollection<ValidationResult> results);
}

/// <summary>
/// 設定データを格納するクラス
/// </summary>
/// <remarks>JSON形式でシリアライズされる</remarks>
internal class ConfigData : IConfigData
{
    // 既存の実装
}
```

### 3. 設定項目の拡張と柔軟性

アプリケーションの将来的な拡張を見据えて、動的な設定項目や追加の設定カテゴリを考慮することも重要です。

```csharp
/// <summary>
/// 設定データを格納するクラス
/// </summary>
/// <remarks>JSON形式でシリアライズされる</remarks>
internal class ConfigData
{
    // 既存のプロパティ
    
    /// <summary>
    /// タイマーの間隔（ミリ秒）
    /// </summary>
    [JsonPropertyName("pollingIntervalMilliseconds")]
    [Range(1000, 60000, ErrorMessage = "Polling interval must be between 1 and 60 seconds")]
    public int PollingIntervalMilliseconds { get; set; } = 5000;
    
    /// <summary>
    /// 通知の設定
    /// </summary>
    [JsonPropertyName("notification")]
    public NotificationSettings Notification { get; set; } = new NotificationSettings();
    
    /// <summary>
    /// 追加の設定（将来の拡張用）
    /// </summary>
    [JsonPropertyName("additionalSettings")]
    public Dictionary<string, JsonElement>? AdditionalSettings { get; set; }
}

/// <summary>
/// 通知関連の設定
/// </summary>
internal class NotificationSettings
{
    /// <summary>
    /// Discord通知を有効にするかどうか
    /// </summary>
    [JsonPropertyName("enableDiscord")]
    public bool EnableDiscord { get; set; } = true;
    
    /// <summary>
    /// Windows通知を有効にするかどうか
    /// </summary>
    [JsonPropertyName("enableWindows")]
    public bool EnableWindows { get; set; } = true;
    
    /// <summary>
    /// 通知に含めるユーザー情報のレベル
    /// </summary>
    [JsonPropertyName("userInfoLevel")]
    [Range(0, 2, ErrorMessage = "User info level must be between 0 and 2")]
    public int UserInfoLevel { get; set; } = 1;
}
```

### 4. イミュータブルな設定の検討

設定の整合性と安全性を高めるために、イミュータブル（不変）な設定クラスの使用を検討すべきです。

```csharp
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
    public string DatabasePath { get; }

    // 他のプロパティ...
    
    /// <summary>
    /// デフォルトコンストラクタ（シリアライズ用）
    /// </summary>
    [JsonConstructor]
    public ConfigData(
        string databasePath = "",
        string discordWebhookUrl = "",
        bool notifyOnStart = true,
        bool notifyOnExit = true,
        int locationCount = 5)
    {
        DatabasePath = databasePath;
        DiscordWebhookUrl = discordWebhookUrl;
        NotifyOnStart = notifyOnStart;
        NotifyOnExit = notifyOnExit;
        LocationCount = locationCount;
    }
    
    /// <summary>
    /// 指定したプロパティを更新した新しいインスタンスを作成する
    /// </summary>
    public ConfigData With(
        string? databasePath = null,
        string? discordWebhookUrl = null,
        bool? notifyOnStart = null,
        bool? notifyOnExit = null,
        int? locationCount = null)
    {
        return new ConfigData(
            databasePath ?? DatabasePath,
            discordWebhookUrl ?? DiscordWebhookUrl,
            notifyOnStart ?? NotifyOnStart,
            notifyOnExit ?? NotifyOnExit,
            locationCount ?? LocationCount);
    }
}
```

### 5. レコード型の利用

.NET 6以降では、レコード型を使用して設定クラスを簡潔に定義することができます。

```csharp
/// <summary>
/// 設定データを格納するレコード
/// </summary>
/// <remarks>JSON形式でシリアライズされる</remarks>
internal record ConfigData(
    [property: JsonPropertyName("databasePath")] string DatabasePath = "",
    [property: JsonPropertyName("discordWebhookUrl")] string DiscordWebhookUrl = "",
    [property: JsonPropertyName("notifyOnStart")] bool NotifyOnStart = true,
    [property: JsonPropertyName("notifyOnExit")] bool NotifyOnExit = true,
    [property: JsonPropertyName("locationCount")] int LocationCount = 5);
```

### 6. クラスの改行の修正

クラス定義の後に空行が挿入されています。これは不要な改行であり、コードスタイルの一貫性のために修正すべきです。

```diff
namespace VRCXDiscordTracker.Core.Config;
/// <summary>
/// 設定データを格納するクラス
/// </summary>
/// <remarks>JSON形式でシリアライズされる</remarks>
- internal class ConfigData
-
- {
+ internal class ConfigData
+ {
    // プロパティ実装
}
```

## セキュリティ上の懸念点

現状では特に大きなセキュリティ上の懸念点はありませんが、機密情報（DiscordWebhookURL）が平文で保存されている点には注意が必要です。

## 総合評価

`ConfigData`クラスは基本的な機能を提供していますが、バリデーション、インターフェースの導入、設定の拡張性、イミュータブルな設計、最新の言語機能の活用などの面で改善の余地があります。特に、アプリケーションの拡張に伴って設定も拡張されることを想定した設計を検討することが重要です。また、微小な問題ですが、クラス定義の空行についても修正が必要です。
