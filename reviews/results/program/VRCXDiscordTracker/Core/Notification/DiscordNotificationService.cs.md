# DiscordNotificationService.cs レビュー

## 概要

`DiscordNotificationService.cs`はDiscord Webhookを使用してVRChat関連の通知を送信する機能を提供するクラスです。インスタンス情報の通知、アプリケーションの起動/終了通知などを担当し、メッセージの新規送信や更新を行います。

## 良い点

1. 責務が明確で、通知機能に特化したクラスになっている
2. メッセージの重複送信を避ける工夫がされている（_lastMessageContent辞書によるキャッシュ）
3. タイムスタンプを除いた比較を行うEqualEmbedWithoutTimestampメソッドなど、細かい配慮がある
4. 例外処理が概ね適切に実装されている
5. XMLドキュメントコメントが充実している

## 改善点

### 1. インターフェースの導入

テスト容易性や拡張性のために、インターフェースを導入すべきです。

```csharp
/// <summary>
/// Discord通知サービスのインターフェース
/// </summary>
internal interface IDiscordNotificationService
{
    /// <summary>
    /// メッセージを送信または更新する
    /// </summary>
    Task SendUpdateMessageAsync();
    
    /// <summary>
    /// アプリケーション起動メッセージを送信する
    /// </summary>
    Task SendAppStartMessageAsync();
    
    /// <summary>
    /// アプリケーション終了メッセージを送信する
    /// </summary>
    Task SendAppExitMessageAsync();
}

/// <summary>
/// Discord通知サービスの実装
/// </summary>
internal class DiscordNotificationService : IDiscordNotificationService
{
    // 実装...
}
```

### 2. 静的メソッドと非静的メソッドの混在を解消

現在、クラスに静的メソッドと非静的メソッドが混在しており、責務の分離が不明確です。インスタンスに関連する通知と、アプリケーション全体に関連する通知を分離すべきです。

```csharp
/// <summary>
/// アプリケーション全体の通知を担当するサービス
/// </summary>
internal static class AppNotificationService
{
    /// <summary>
    /// アプリケーションの起動メッセージを送信する
    /// </summary>
    public static async Task SendAppStartMessageAsync()
    {
        // 既存の実装
    }
    
    /// <summary>
    /// アプリケーションの終了メッセージを送信する
    /// </summary>
    public static async Task SendAppExitMessageAsync()
    {
        // 既存の実装
    }
}

/// <summary>
/// インスタンス情報の通知を担当するサービス
/// </summary>
internal class InstanceNotificationService
{
    // 非静的メソッド
}
```

### 3. メッセージID保存の堅牢性強化

現在、JoinIDとMessageIDのペアを単純なJSONファイルに保存していますが、ファイル操作の失敗に対する対策が不十分です。より堅牢なデータ保存機構を検討すべきです。

```csharp
/// <summary>
/// JoinIdとMessageIdのペアを保存する辞書をロードする
/// </summary>
private static Dictionary<string, ulong> LoadJoinIdMessageIdPairs()
{
    if (!File.Exists(_saveFilePath))
    {
        return [];
    }

    try
    {
        var json = File.ReadAllText(_saveFilePath);
        var result = JsonSerializer.Deserialize<Dictionary<string, ulong>>(json);
        if (result == null)
        {
            Console.WriteLine("Failed to deserialize message pairs. Using empty dictionary.");
            return [];
        }
        return result;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"I/O error loading message pairs: {ex.Message}");
        return [];
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"JSON error loading message pairs: {ex.Message}");
        // 破損ファイルをバックアップ
        try
        {
            File.Copy(_saveFilePath, $"{_saveFilePath}.bak", true);
        }
        catch
        {
            // バックアップの失敗はログのみ
            Console.WriteLine("Failed to backup corrupted message pairs file.");
        }
        return [];
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error loading message pairs: {ex.Message}");
        return [];
    }
}
```

### 4. メッセージ保存パスのカスタマイズ

メッセージIDの保存パスが現在のディレクトリにハードコードされています。これを設定可能とすべきです。

```csharp
/// <summary>
/// 保存パス
/// </summary>
private static readonly string _saveFilePath = DetermineSaveFilePath();

/// <summary>
/// 保存パスを決定する
/// </summary>
private static string DetermineSaveFilePath()
{
    // 設定から取得するか、環境に応じて自動決定
    string configPath = AppConfig.MessageStoragePath;
    if (!string.IsNullOrEmpty(configPath))
    {
        return configPath;
    }
    
    // ポータブルモード（同じディレクトリにportable.txtが存在する場合）
    if (File.Exists(Path.Combine(Environment.CurrentDirectory, "portable.txt")))
    {
        return Path.Combine(Environment.CurrentDirectory, "discord-messages.json");
    }
    
    // インストールモード
    return Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppConstants.AppName,
        "discord-messages.json");
}
```

### 5. 並行処理のサポート強化

複数の通知が同時に処理される可能性があり、特にファイル操作時の競合が発生する恐れがあります。排他制御を追加すべきです。

```csharp
/// <summary>
/// ファイル操作のロックオブジェクト
/// </summary>
private static readonly object _fileLock = new();

/// <summary>
/// JoinIdとMessageIdのペアを保存する
/// </summary>
private static void SaveJoinIdMessageIdPairs()
{
    lock (_fileLock)
    {
        try
        {
            // ディレクトリが存在しない場合は作成
            string? directory = Path.GetDirectoryName(_saveFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(_joinIdMessageIdPairs, _jsonSerializerOptions);
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving joinIdMessageIdPairs: {ex.Message}");
        }
    }
}
```

### 6. Webhook URLのバリデーション強化

現在、Webhook URLの空文字チェックのみ行っていますが、より厳密な検証を追加すべきです。

```csharp
/// <summary>
/// Webhook URLを検証する
/// </summary>
/// <param name="url">検証対象のURL</param>
/// <returns>有効な場合はtrue、無効な場合はfalse</returns>
private static bool ValidateWebhookUrl(string url)
{
    if (string.IsNullOrEmpty(url))
        return false;
        
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        return false;
        
    // Discordドメインのチェック
    if (!uri.Host.EndsWith("discord.com", StringComparison.OrdinalIgnoreCase))
        return false;
        
    // Webhook APIパスのチェック
    return uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase);
}

// 使用例
private static async Task<ulong?> SendNewMessage(Embed embed)
{
    var url = AppConfig.DiscordWebhookUrl;
    if (!ValidateWebhookUrl(url)) 
    {
        Console.WriteLine("Invalid webhook URL");
        return null;
    }
    
    // 送信処理...
}
```

### 7. メッセージキャッシュの管理

`_lastMessageContent`辞書が無限に大きくなる可能性があります。キャッシュのサイズ制限や定期的なクリーンアップを実装すべきです。

```csharp
/// <summary>
/// キャッシュの最大サイズ
/// </summary>
private const int MAX_CACHE_SIZE = 100;

/// <summary>
/// メッセージキャッシュをクリーンアップする
/// </summary>
private static void CleanupMessageCache()
{
    if (_lastMessageContent.Count <= MAX_CACHE_SIZE)
        return;
        
    // 最も古いエントリから削除（簡易実装）
    var oldestEntries = _lastMessageContent.Keys.Take(_lastMessageContent.Count - MAX_CACHE_SIZE);
    foreach (var key in oldestEntries)
    {
        _lastMessageContent.Remove(key);
    }
}

// UpdateMessageメソッド内での使用例
private static async Task<bool> UpdateMessage(ulong messageId, Embed embed)
{
    // 既存の処理...
    
    _lastMessageContent[messageId] = embed;
    CleanupMessageCache();
    
    // 残りの処理...
}
```

### 8. エラーログの改善

現在、エラーログが非構造化されたConsole出力のみです。より詳細なロギングと、定型的なエラーメッセージを実装すべきです。

```csharp
/// <summary>
/// エラーをログに記録する
/// </summary>
/// <param name="context">エラーのコンテキスト</param>
/// <param name="exception">発生した例外</param>
private static void LogError(string context, Exception exception)
{
    Console.WriteLine($"[ERROR] Discord notification - {context}: {exception.Message}");
    Console.WriteLine($"  Type: {exception.GetType().Name}");
    Console.WriteLine($"  Stack: {exception.StackTrace?.Split('\n')[0]}");
    
    // さらに詳細なログを実装することも検討
}

// 使用例
private static async Task<bool> UpdateMessage(ulong messageId, Embed embed)
{
    // 既存のコード...
    
    try
    {
        await client.ModifyMessageAsync(messageId, m => m.Embeds = new[] { embed });
        _lastMessageContent[messageId] = embed;
        return true;
    }
    catch (Exception ex)
    {
        LogError($"Failed to update message {messageId}", ex);
        return false;
    }
}
```

## セキュリティ上の懸念点

1. Webhook URLが平文で設定ファイルに保存されている点（AppConfigクラスの問題）
2. ファイル操作時の適切なパーミッション確認が行われていない
3. メッセージ内容によっては、センシティブな情報がDiscordに送信される可能性がある

## 総合評価

`DiscordNotificationService`クラスは基本的な機能を果たしていますが、テスト容易性、エラー処理、並行性、設定管理などの面で改善の余地があります。特に、静的メソッドと非静的メソッドの混在による責務の不明確さは、早急に対応すべき問題です。また、エラー処理の強化とファイル操作の堅牢性向上も重要な改善点です。

クラス設計としては、インターフェースを導入し、責務をより明確に分離することで、より保守性の高いコードになるでしょう。また、設定の外部化や柔軟性の向上も検討すべきです。
