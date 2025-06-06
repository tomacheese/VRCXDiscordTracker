# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/Notification/UwpNotificationService.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 19行（小規模）

## 概要
Windows トースト通知を表示する単純なサービスクラス。Microsoft.Toolkit.Uwp.Notificationsを使用している。

## 総合評価
**スコア: 7/10**

シンプルで目的が明確だが、エラーハンドリングと設計の改善余地がある。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐⭐⭐☆
**Good:**
- 単一責任の原則に従った設計
- シンプルで理解しやすい
- 適切な外部ライブラリ使用

**Issues:**
- 静的メソッドでテスト困難
- 設定の外部化不可

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐⭐
**Good:**
- C#命名規約に準拠
- 適切なXMLドキュメンテーション

### 3. セキュリティ上の問題 ⭐⭐⭐⭐⭐
**Good:**
- セキュリティ上の問題なし
- 入力は適切に処理される

### 4. パフォーマンスの問題 ⭐⭐⭐⭐⭐
**Good:**
- 軽量な処理
- パフォーマンス上の問題なし

### 5. 可読性・保守性 ⭐⭐⭐⭐☆
**Good:**
- 明確で簡潔なコード

**Issues:**
- 設定の柔軟性不足

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- 静的メソッドでモック化困難
- 外部UIライブラリへの直接依存

## 具体的な問題点と改善提案

### 1. 【重要度：中】エラーハンドリングの追加
**問題**: 通知失敗時の例外処理なし

**改善案**:
```csharp
/// <summary>
/// UWP通知サービス
/// </summary>
internal class UwpNotificationService
{
    /// <summary>
    /// Windowsのトースト通知を表示する
    /// </summary>
    /// <param name="title">通知のタイトル</param>
    /// <param name="message">通知のメッセージ</param>
    /// <returns>通知が正常に表示された場合はtrue</returns>
    public static bool TryNotify(string title, string message)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
            ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));

            Console.WriteLine("UwpNotificationService.Notify()");
            
            new ToastContentBuilder()
                .AddText(SanitizeText(title))
                .AddText(SanitizeText(message))
                .Show();
                
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to show toast notification: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 非同期でトースト通知を表示する
    /// </summary>
    public static async Task<bool> TryNotifyAsync(string title, string message)
    {
        return await Task.Run(() => TryNotify(title, message));
    }

    /// <summary>
    /// 既存のNotifyメソッド（互換性のため保持）
    /// </summary>
    public static void Notify(string title, string message)
    {
        if (!TryNotify(title, message))
        {
            throw new NotificationException($"Failed to display notification: {title}");
        }
    }

    /// <summary>
    /// テキストのサニタイズ処理
    /// </summary>
    private static string SanitizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 長すぎるテキストをトリム
        const int maxLength = 200;
        if (text.Length > maxLength)
        {
            text = text[..(maxLength - 3)] + "...";
        }

        return text.Trim();
    }
}

/// <summary>
/// 通知関連の例外
/// </summary>
public class NotificationException : Exception
{
    public NotificationException(string message) : base(message) { }
    public NotificationException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 2. 【重要度：中】インターフェース抽出とDI対応
**改善案**:
```csharp
/// <summary>
/// 通知サービスのインターフェース
/// </summary>
public interface INotificationService
{
    Task<bool> ShowNotificationAsync(string title, string message);
    Task<bool> ShowNotificationAsync(NotificationData notification);
    bool IsAvailable { get; }
}

/// <summary>
/// UWP通知サービスの実装
/// </summary>
internal class UwpNotificationService : INotificationService
{
    private readonly NotificationSettings _settings;
    private readonly ILogger<UwpNotificationService> _logger;

    public UwpNotificationService(NotificationSettings settings, ILogger<UwpNotificationService> logger)
    {
        _settings = settings ?? new NotificationSettings();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsAvailable => OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10);

    public async Task<bool> ShowNotificationAsync(string title, string message)
    {
        var notification = new NotificationData(title, message);
        return await ShowNotificationAsync(notification);
    }

    public async Task<bool> ShowNotificationAsync(NotificationData notification)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("UWP notifications are not available on this platform");
            return false;
        }

        try
        {
            await Task.Run(() =>
            {
                var builder = new ToastContentBuilder()
                    .AddText(SanitizeText(notification.Title, _settings.MaxTitleLength))
                    .AddText(SanitizeText(notification.Message, _settings.MaxMessageLength));

                if (!string.IsNullOrEmpty(notification.AppLogo))
                {
                    builder.AddAppLogoOverride(new Uri(notification.AppLogo));
                }

                if (_settings.DisplayDuration.HasValue)
                {
                    builder.SetToastDuration(ToastDuration.Long);
                }

                builder.Show();
            });

            _logger.LogDebug("Toast notification displayed: {Title}", notification.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display toast notification: {Title}", notification.Title);
            return false;
        }
    }

    private string SanitizeText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim();
        if (text.Length > maxLength)
        {
            text = text[..(maxLength - 3)] + "...";
        }

        return text;
    }
}

/// <summary>
/// 通知データ
/// </summary>
public record NotificationData(string Title, string Message)
{
    public string? AppLogo { get; init; }
    public string? ActionUrl { get; init; }
    public DateTime? Timestamp { get; init; }
}

/// <summary>
/// 通知設定
/// </summary>
public class NotificationSettings
{
    public int MaxTitleLength { get; set; } = 50;
    public int MaxMessageLength { get; set; } = 200;
    public TimeSpan? DisplayDuration { get; set; }
    public bool EnableSounds { get; set; } = true;
    public string? DefaultAppLogo { get; set; }
}
```

### 3. 【重要度：低】高度な通知機能の追加
**改善案**:
```csharp
/// <summary>
/// 拡張UWP通知サービス
/// </summary>
internal class ExtendedUwpNotificationService : UwpNotificationService
{
    public ExtendedUwpNotificationService(NotificationSettings settings, ILogger<ExtendedUwpNotificationService> logger)
        : base(settings, logger) { }

    /// <summary>
    /// アクション付き通知を表示
    /// </summary>
    public async Task<bool> ShowNotificationWithActionsAsync(
        string title, 
        string message, 
        params NotificationAction[] actions)
    {
        try
        {
            await Task.Run(() =>
            {
                var builder = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message);

                foreach (var action in actions.Take(5)) // Windowsの制限
                {
                    builder.AddButton(new ToastButton()
                        .SetContent(action.Text)
                        .AddArgument("action", action.Id));
                }

                builder.Show();
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display notification with actions");
            return false;
        }
    }

    /// <summary>
    /// プログレス付き通知を表示
    /// </summary>
    public async Task<bool> ShowProgressNotificationAsync(
        string title, 
        string message, 
        double progress = 0.0)
    {
        try
        {
            await Task.Run(() =>
            {
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .AddVisualChild(new AdaptiveProgressBar()
                    {
                        Value = new BindableProgressBarValue("progressValue"),
                        ValueStringOverride = new BindableString("progressValueString"),
                        Status = new BindableString("progressStatus")
                    })
                    .AddToastActivationInfo("progressData", ToastActivationType.Foreground)
                    .Show();
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display progress notification");
            return false;
        }
    }
}

/// <summary>
/// 通知アクション
/// </summary>
public record NotificationAction(string Id, string Text)
{
    public string? Arguments { get; init; }
    public string? ImageUri { get; init; }
}
```

### 4. 【重要度：低】設定可能な通知サービス
**改善案**:
```csharp
/// <summary>
/// 設定可能な通知サービス
/// </summary>
internal class ConfigurableNotificationService : INotificationService
{
    private readonly List<INotificationService> _notificationServices;
    private readonly NotificationSettings _settings;

    public ConfigurableNotificationService(
        IEnumerable<INotificationService> notificationServices,
        NotificationSettings settings)
    {
        _notificationServices = notificationServices.Where(s => s.IsAvailable).ToList();
        _settings = settings;
    }

    public bool IsAvailable => _notificationServices.Any();

    public async Task<bool> ShowNotificationAsync(string title, string message)
    {
        var notification = new NotificationData(title, message);
        return await ShowNotificationAsync(notification);
    }

    public async Task<bool> ShowNotificationAsync(NotificationData notification)
    {
        var results = new List<bool>();

        foreach (var service in _notificationServices)
        {
            try
            {
                var result = await service.ShowNotificationAsync(notification);
                results.Add(result);
            }
            catch (Exception)
            {
                results.Add(false);
            }
        }

        return results.Any(r => r); // 少なくとも1つが成功すればOK
    }
}
```

## 推奨されるNext Steps
1. エラーハンドリングの追加（中優先度）
2. インターフェース抽出とDI対応（中優先度）
3. 通知設定の外部化（低優先度）
4. 単体テストの追加（低優先度）

## コメント
シンプルで目的が明確な実装です。UWP通知の基本機能は適切に動作しますが、プロダクション環境では例外処理の強化とテスタビリティの向上が重要です。特に通知が失敗した場合のフォールバック機能や、異なるプラットフォームでの動作考慮があると良いでしょう。現状でも十分実用的ですが、DI対応により他の通知方式との組み合わせが可能になります。