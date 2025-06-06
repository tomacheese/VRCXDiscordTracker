# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/Config/ConfigData.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 40行（小規模）

## 概要
設定データを格納するデータクラス。JSON シリアライゼーション用の属性とデフォルト値を定義している。

## 総合評価
**スコア: 8/10**

基本的な設計は優秀だが、バリデーションと型安全性の観点で改善の余地がある。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐⭐⭐⭐
**Good:**
- シンプルで明確なデータクラス
- 適切なJSON属性の使用
- 責任が明確（データ保持のみ）

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐⭐
**Good:**
- C#命名規約に準拠
- 適切なXMLドキュメンテーション
- JSON属性の統一された使用

### 3. セキュリティ上の問題 ⭐⭐⭐⭐☆
**Good:**
- 機密情報の適切な扱い

**Minor Issues:**
- バリデーション機能の欠如

### 4. パフォーマンスの問題 ⭐⭐⭐⭐⭐
**Good:**
- 軽量なデータ構造
- 効率的なシリアライゼーション

### 5. 可読性・保守性 ⭐⭐⭐⭐⭐
**Good:**
- 明確な命名とコメント
- 簡潔な構造

### 6. テスト容易性 ⭐⭐⭐⭐⭐
**Good:**
- シンプルな構造でテストしやすい
- 副作用なし

## 具体的な問題点と改善提案

### 1. 【重要度：中】バリデーション機能の追加
**改善案**:
```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VRCXDiscordTracker.Core.Config;

/// <summary>
/// 設定データを格納するクラス
/// </summary>
/// <remarks>JSON形式でシリアライズされる</remarks>
internal class ConfigData : IValidatableObject
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
    [Range(1, 100, ErrorMessage = "Location count must be between 1 and 100")]
    public int LocationCount { get; set; } = 5;

    /// <summary>
    /// 設定データの検証を行う
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // データベースパスの検証
        if (!string.IsNullOrEmpty(DatabasePath) && !IsValidDatabasePath(DatabasePath))
        {
            results.Add(new ValidationResult(
                "Database path must be a valid SQLite file path",
                new[] { nameof(DatabasePath) }));
        }

        // Webhook URLの検証
        if (!string.IsNullOrEmpty(DiscordWebhookUrl) && !IsValidWebhookUrl(DiscordWebhookUrl))
        {
            results.Add(new ValidationResult(
                "Discord webhook URL must be a valid HTTP/HTTPS URL",
                new[] { nameof(DiscordWebhookUrl) }));
        }

        return results;
    }

    /// <summary>
    /// 設定が有効かどうかを確認
    /// </summary>
    public bool IsValid => !Validate(new ValidationContext(this)).Any();

    /// <summary>
    /// デフォルト値で初期化された新しいインスタンスを作成
    /// </summary>
    public static ConfigData CreateDefault() => new()
    {
        DatabasePath = string.Empty, // AppConfigで解決される
        DiscordWebhookUrl = string.Empty,
        NotifyOnStart = true,
        NotifyOnExit = true,
        LocationCount = 5
    };

    private static bool IsValidDatabasePath(string path)
    {
        try
        {
            return Path.IsPathFullyQualified(path) &&
                   Path.GetExtension(path).Equals(".sqlite3", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidWebhookUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == "http" || uri.Scheme == "https");
    }
}
```

### 2. 【重要度：中】record への変更検討
**改善案**: 不変データクラスとしての実装
```csharp
/// <summary>
/// 設定データを格納するレコード（不変版）
/// </summary>
/// <param name="DatabasePath">VRCXのデータベースファイルのパス</param>
/// <param name="DiscordWebhookUrl">DiscordのWebhook URL</param>
/// <param name="NotifyOnStart">アプリケーション起動時の通知可否</param>
/// <param name="NotifyOnExit">アプリケーション終了時の通知可否</param>
/// <param name="LocationCount">通知対象とするロケーションの数</param>
internal record ConfigData(
    [property: JsonPropertyName("databasePath")] string DatabasePath = "",
    [property: JsonPropertyName("discordWebhookUrl")] string DiscordWebhookUrl = "",
    [property: JsonPropertyName("notifyOnStart")] bool NotifyOnStart = true,
    [property: JsonPropertyName("notifyOnExit")] bool NotifyOnExit = true,
    [property: JsonPropertyName("locationCount")] int LocationCount = 5
)
{
    /// <summary>
    /// 設定データの検証
    /// </summary>
    public bool IsValid =>
        IsValidLocationCount(LocationCount) &&
        IsValidDatabasePath(DatabasePath) &&
        IsValidWebhookUrl(DiscordWebhookUrl);

    /// <summary>
    /// 検証メッセージの取得
    /// </summary>
    public IEnumerable<string> GetValidationErrors()
    {
        if (!IsValidLocationCount(LocationCount))
            yield return $"Location count must be between 1 and 100, got {LocationCount}";

        if (!string.IsNullOrEmpty(DatabasePath) && !IsValidDatabasePath(DatabasePath))
            yield return $"Invalid database path: {DatabasePath}";

        if (!string.IsNullOrEmpty(DiscordWebhookUrl) && !IsValidWebhookUrl(DiscordWebhookUrl))
            yield return $"Invalid Discord webhook URL: {DiscordWebhookUrl}";
    }

    private static bool IsValidLocationCount(int count) => count >= 1 && count <= 100;
    // 他の検証メソッド...
}
```

### 3. 【重要度：低】型安全性の向上
**改善案**: より強い型定義
```csharp
/// <summary>
/// 強い型付きの設定データ
/// </summary>
internal class TypedConfigData
{
    /// <summary>
    /// データベースパス（検証済み）
    /// </summary>
    [JsonPropertyName("databasePath")]
    public DatabasePath DatabasePath { get; set; } = DatabasePath.Default;

    /// <summary>
    /// Webhook URL（検証済み）
    /// </summary>
    [JsonPropertyName("discordWebhookUrl")]
    public WebhookUrl DiscordWebhookUrl { get; set; } = WebhookUrl.Empty;

    /// <summary>
    /// ロケーション数（検証済み）
    /// </summary>
    [JsonPropertyName("locationCount")]
    public LocationCount LocationCount { get; set; } = LocationCount.Default;

    // 他のプロパティ...
}

/// <summary>
/// データベースパスを表すValue Object
/// </summary>
public readonly record struct DatabasePath
{
    private readonly string _value;

    public DatabasePath(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException($"Invalid database path: {value}");
        _value = value;
    }

    public static DatabasePath Default => new(AppConstants.VRCXDefaultDatabasePath);

    public override string ToString() => _value;

    public static implicit operator string(DatabasePath path) => path._value;
    
    private static bool IsValid(string path) =>
        !string.IsNullOrEmpty(path) &&
        Path.IsPathFullyQualified(path) &&
        Path.GetExtension(path).Equals(".sqlite3", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// ロケーション数を表すValue Object
/// </summary>
public readonly record struct LocationCount
{
    private readonly int _value;

    public LocationCount(int value)
    {
        if (value < 1 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Location count must be between 1 and 100");
        _value = value;
    }

    public static LocationCount Default => new(5);

    public override string ToString() => _value.ToString();

    public static implicit operator int(LocationCount count) => count._value;
}
```

### 4. 【重要度：低】JSON スキーマ対応
**改善案**:
```csharp
/// <summary>
/// JSON Schema 対応の設定データ
/// </summary>
[JsonSchemaExporter.GenerateSchema]
internal class ConfigDataWithSchema : ConfigData
{
    /// <summary>
    /// JSON Schema のバージョン
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://json-schema.org/draft/2020-12/schema";

    /// <summary>
    /// 設定ファイルのバージョン
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// マイグレーション情報
    /// </summary>
    public static ConfigDataWithSchema MigrateFrom(ConfigData legacy)
    {
        return new ConfigDataWithSchema
        {
            DatabasePath = legacy.DatabasePath,
            DiscordWebhookUrl = legacy.DiscordWebhookUrl,
            NotifyOnStart = legacy.NotifyOnStart,
            NotifyOnExit = legacy.NotifyOnExit,
            LocationCount = legacy.LocationCount
        };
    }
}
```

## 推奨されるNext Steps
1. バリデーション機能の追加（中優先度）
2. record への変更検討（中優先度）
3. 型安全性の向上（低優先度）
4. JSON スキーマ対応（低優先度）

## コメント
シンプルで明確なデータクラスとして適切に設計されています。JSON シリアライゼーション属性の使用も適切で、可読性も高いです。改善案として、バリデーション機能の追加や不変データクラス（record）への変更を検討することで、より堅牢なデータモデルにできます。現状でも十分実用的ですが、より大規模なアプリケーションへの発展を考慮すると、型安全性の向上も価値があります。