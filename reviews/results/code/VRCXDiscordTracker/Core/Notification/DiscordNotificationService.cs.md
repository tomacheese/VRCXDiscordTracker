# コードレビュー: VRCXDiscordTracker/Core/Notification/DiscordNotificationService.cs

## 概要

このクラスはDiscord Webhookを利用して、VRCXのインスタンス情報や位置情報をDiscordチャンネルに通知するサービスを提供しています。

## 良い点

- XMLドキュメントコメントが適切に記述されており、メソッドの目的と使用方法が明確です。
- C# 12のプライマリコンストラクターを使用しており、最新の言語機能を活用しています。
- メッセージのキャッシュを実装し、同一内容の送信を避けることでAPIの負荷を軽減しています。
- 例外処理が実装されており、エラー発生時の復旧処理が考慮されています。

## 改善点

### 1. ファイルパス管理

```csharp
// ファイルパスがハードコードされています
private static readonly string _saveFilePath = "discord-messages.json";

// 設定から読み込むか、環境に応じたパスを使用するべきです
private static readonly string _saveFilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    AppConstants.AppName,
    "discord-messages.json");
```

### 2. 静的変数とスレッド安全性

```csharp
// 静的な辞書が複数のスレッドからアクセスされる可能性があり、
// スレッド安全ではありません
private static readonly Dictionary<string, ulong> _joinIdMessageIdPairs = LoadJoinIdMessageIdPairs();
private static readonly Dictionary<ulong, Embed> _lastMessageContent = [];

// 以下のようにスレッド安全な辞書を使用するべきです
private static readonly ConcurrentDictionary<string, ulong> _joinIdMessageIdPairs = 
    new ConcurrentDictionary<string, ulong>(LoadJoinIdMessageIdPairs());
private static readonly ConcurrentDictionary<ulong, Embed> _lastMessageContent = 
    new ConcurrentDictionary<ulong, Embed>();
```

### 3. 例外処理

```csharp
// 例外をキャッチした後のログ出力のみで、追加対応がありません
catch (Exception ex)
{
    Console.WriteLine($"Error updating message: {ex.Message}");
    return false;
}

// より詳細な例外処理と適切なリカバリーを実装するべきです
catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
{
    // メッセージが見つからない場合（削除された場合など）
    Logger.LogWarning($"Message {messageId} not found: {ex.Message}");
    _joinIdMessageIdPairs.Remove(GetJoinId());
    SaveJoinIdMessageIdPairs();
    return false;
}
catch (Exception ex)
{
    Logger.LogError($"Error updating message: {ex.Message}");
    // 重大なエラーの場合は、後でリトライできるように状態を記録
    return false;
}
```

### 4. ファイル操作とエラーハンドリング

```csharp
// ファイル操作が例外をキャッチするだけで、追加の処理がありません
private static void SaveJoinIdMessageIdPairs()
{
    try
    {
        var json = JsonSerializer.Serialize(_joinIdMessageIdPairs, _jsonSerializerOptions);
        File.WriteAllText(_saveFilePath, json);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving joinIdMessageIdPairs: {ex.Message}");
    }
}

// より堅牢なファイル操作を実装するべきです
private static void SaveJoinIdMessageIdPairs()
{
    try
    {
        // ディレクトリが存在することを確認
        var directory = Path.GetDirectoryName(_saveFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 一時ファイルに書き込んでから移動することで、書き込み中のクラッシュによる
        // ファイル破損を防止
        var tempFile = Path.GetTempFileName();
        var json = JsonSerializer.Serialize(_joinIdMessageIdPairs, _jsonSerializerOptions);
        File.WriteAllText(tempFile, json);
        File.Move(tempFile, _saveFilePath, overwrite: true);
    }
    catch (Exception ex)
    {
        Logger.LogError($"Error saving message mappings: {ex.Message}", ex);
        // エラーが発生した場合にUIに通知する処理を追加
    }
}
```

### 5. 依存性の注入

```csharp
// サービスが直接設定にアクセスしています
var url = AppConfig.DiscordWebhookUrl;

// 依存性注入パターンを使用するべきです
private readonly IAppConfig _appConfig;

public DiscordNotificationService(MyLocation myLocation, 
                                  List<InstanceMember> instanceMembers,
                                  IAppConfig appConfig)
{
    this.myLocation = myLocation;
    this.instanceMembers = instanceMembers;
    _appConfig = appConfig;
}

// 使用時
var url = _appConfig.DiscordWebhookUrl;
```

## セキュリティの問題

- Webhookの URL がアプリケーション設定から直接読み取られ、ログにも出力される可能性があります。機密情報は適切にマスクするべきです。
- discord-messages.json ファイルがカレントディレクトリに保存されており、アクセス制限がありません。適切なユーザーデータディレクトリに保存するべきです。

## パフォーマンスの問題

- メッセージを送信する度に新しいDiscordWebhookClientを作成していますが、再利用することで接続オーバーヘッドを減らせる可能性があります。
- 同一のメッセージ内容を検証するためのEqualEmbedWithoutTimestampメソッドが効率的でない可能性があります。より効率的な比較方法を検討してください。

## テスト容易性

- 静的メンバーやメソッドが多用されており、単体テストが困難です。インターフェースと依存性注入を使用してテスト可能にするべきです。
- 直接ファイルシステムにアクセスする部分をモック可能なインターフェースに抽象化することで、テストが容易になります。

## その他のコメント

- ローカライズされていないメッセージ（"Application has started." など）が含まれています。リソースファイルを使用して多言語対応するべきです。

```csharp
// 現状のハードコードされたメッセージ
Description = "Application has started.",

// 改善案
Description = Resources.ApplicationStarted,
```

- サービスはDiscord Webhookに強く依存しており、他の通知サービスへの拡張が難しい設計になっています。通知サービスを抽象化することを検討してください。

```csharp
// 通知インターフェースを定義
public interface INotificationService
{
    Task SendNotificationAsync(string title, string message, NotificationType type);
    Task UpdateNotificationAsync(string id, string title, string message, NotificationType type);
}

// Discord実装
public class DiscordNotificationService : INotificationService
{
    // 実装
}

// 他の通知サービス実装（例：Slack）
public class SlackNotificationService : INotificationService
{
    // 実装
}
```

- Embedオブジェクトの直接シリアル化とデシリアライズを行っていませんが、もし必要になった場合はカスタムのシリアライザーが必要になる可能性があります。Discord.NETのEmbedクラスは複雑なオブジェクトであるため、JSONのシリアライズ/デシリアライズに問題が生じる可能性があります。

- メッセージの一意性判定にJoinIdを使用していますが、これがユニークでなくなる（同じワールドに複数回訪問するなど）ケースがあります。より堅牢な識別子の使用を検討すべきです。
