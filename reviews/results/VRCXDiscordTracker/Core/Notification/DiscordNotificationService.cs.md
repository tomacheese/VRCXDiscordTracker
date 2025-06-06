# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/Notification/DiscordNotificationService.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 217行（大規模）

## 概要
Discord Webhookを使用した通知サービス。メッセージの送信・更新、永続化、重複送信防止機能を実装している。

## 総合評価
**スコア: 6/10**

基本機能は適切だが、設計、エラーハンドリング、リソース管理の観点で改善が必要。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐⭐☆☆
**Good:**
- メッセージ更新機能
- 重複送信防止
- 永続化機能

**Issues:**
- 静的フィールドの多用によるグローバル状態
- 単一責任の原則違反
- スレッドセーフティの欠如

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐☆
**Good:**
- C#命名規約に準拠
- XMLドキュメンテーションが適切

**Issues:**
- 長いメソッド
- 複雑な条件分岐

### 3. セキュリティ上の問題 ⭐⭐⭐☆☆
**Good:**
- Webhook URL の検証

**Issues:**
- ファイル永続化時のセキュリティ考慮不足
- メッセージIDの平文保存

### 4. パフォーマンスの問題 ⭐⭐⭐☆☆
**Good:**
- 適切なHTTPクライアント使用

**Issues:**
- 同期的なファイルI/O
- メモリ内キャッシュの無制限増加

### 5. 可読性・保守性 ⭐⭐⭐☆☆
**Good:**
- 明確なメソッド名

**Issues:**
- 複雑な条件分岐
- グローバル状態の管理

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- 静的メソッドとフィールドでテスト困難
- 外部依存への強い結合
- ファイルI/Oの副作用

## 具体的な問題点と改善提案

### 1. 【重要度：高】設計の根本的な改善
**問題**: 静的フィールド、グローバル状態、スレッドセーフティの欠如

**改善案**:
```csharp
/// <summary>
/// Discord通知サービスのインターフェース
/// </summary>
public interface IDiscordNotificationService
{
    Task SendOrUpdateMessageAsync(MyLocation location, List<InstanceMember> members);
    Task SendAppStartMessageAsync();
    Task SendAppExitMessageAsync();
    Task<bool> IsEmbedChangedAsync(string joinId, Embed embed);
}

/// <summary>
/// Discord通知サービスの実装
/// </summary>
internal class DiscordNotificationService : IDiscordNotificationService, IDisposable
{
    private readonly IDiscordWebhookClient _webhookClient;
    private readonly IMessageRepository _messageRepository;
    private readonly IEmbedBuilder _embedBuilder;
    private readonly ILogger<DiscordNotificationService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<ulong, Embed> _lastMessageContent = new();

    public DiscordNotificationService(
        IDiscordWebhookClient webhookClient,
        IMessageRepository messageRepository,
        IEmbedBuilder embedBuilder,
        ILogger<DiscordNotificationService> logger)
    {
        _webhookClient = webhookClient ?? throw new ArgumentNullException(nameof(webhookClient));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _embedBuilder = embedBuilder ?? throw new ArgumentNullException(nameof(embedBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendOrUpdateMessageAsync(MyLocation location, List<InstanceMember> members)
    {
        await _semaphore.WaitAsync();
        try
        {
            var joinId = location.JoinId.ToString(CultureInfo.InvariantCulture);
            var embed = _embedBuilder.BuildEmbed(location, members);
            
            var existingMessageId = await _messageRepository.GetMessageIdAsync(joinId);
            
            if (existingMessageId.HasValue)
            {
                var updateResult = await TryUpdateMessageAsync(existingMessageId.Value, embed);
                if (updateResult)
                {
                    _logger.LogInformation("Message updated successfully for join ID {JoinId}", joinId);
                    return;
                }
                
                _logger.LogWarning("Failed to update message {MessageId}, sending new message", existingMessageId);
            }

            var newMessageId = await SendNewMessageAsync(embed);
            if (newMessageId.HasValue)
            {
                await _messageRepository.SaveMessageIdAsync(joinId, newMessageId.Value);
                _logger.LogInformation("New message sent with ID {MessageId} for join ID {JoinId}", newMessageId, joinId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<bool> TryUpdateMessageAsync(ulong messageId, Embed embed)
    {
        try
        {
            if (_lastMessageContent.TryGetValue(messageId, out var lastEmbed) && 
                EmbedComparer.AreEquivalent(lastEmbed, embed))
            {
                _logger.LogDebug("Embed content unchanged, skipping update for message {MessageId}", messageId);
                return true;
            }

            await _webhookClient.UpdateMessageAsync(messageId, embed);
            _lastMessageContent[messageId] = embed;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update message {MessageId}", messageId);
            return false;
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### 2. 【重要度：高】リポジトリパターンの導入
**改善案**:
```csharp
/// <summary>
/// メッセージデータの永続化インターフェース
/// </summary>
public interface IMessageRepository
{
    Task<ulong?> GetMessageIdAsync(string joinId);
    Task SaveMessageIdAsync(string joinId, ulong messageId);
    Task RemoveMessageIdAsync(string joinId);
    Task CleanupOldEntriesAsync(TimeSpan maxAge);
}

/// <summary>
/// JSON ファイルベースのメッセージリポジトリ
/// </summary>
internal class JsonMessageRepository : IMessageRepository
{
    private readonly string _filePath;
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public JsonMessageRepository(string filePath, IFileSystem fileSystem)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public async Task<ulong?> GetMessageIdAsync(string joinId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            return data.TryGetValue(joinId, out var messageData) ? messageData.MessageId : null;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveMessageIdAsync(string joinId, ulong messageId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            data[joinId] = new MessageData
            {
                MessageId = messageId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            await SaveDataAsync(data);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<Dictionary<string, MessageData>> LoadDataAsync()
    {
        if (!await _fileSystem.FileExistsAsync(_filePath))
            return new Dictionary<string, MessageData>();

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<Dictionary<string, MessageData>>(json) ?? 
                   new Dictionary<string, MessageData>();
        }
        catch (JsonException)
        {
            // 破損したファイルは新しく作り直す
            return new Dictionary<string, MessageData>();
        }
    }

    private async Task SaveDataAsync(Dictionary<string, MessageData> data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await _fileSystem.WriteAllTextAsync(_filePath, json);
    }
}

/// <summary>
/// メッセージデータ
/// </summary>
internal record MessageData
{
    public ulong MessageId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
```

### 3. 【重要度：中】Embedの比較機能改善
**改善案**:
```csharp
/// <summary>
/// Embed比較ユーティリティ
/// </summary>
internal static class EmbedComparer
{
    public static bool AreEquivalent(Embed left, Embed right, bool ignoreTimestamp = true)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;

        var leftBuilder = left.ToEmbedBuilder();
        var rightBuilder = right.ToEmbedBuilder();

        if (ignoreTimestamp)
        {
            leftBuilder.Timestamp = null;
            rightBuilder.Timestamp = null;
        }

        return AreEmbedBuildersEqual(leftBuilder, rightBuilder);
    }

    private static bool AreEmbedBuildersEqual(EmbedBuilder left, EmbedBuilder right)
    {
        return left.Title == right.Title &&
               left.Description == right.Description &&
               left.Color == right.Color &&
               left.Url == right.Url &&
               AreAuthorsEqual(left.Author, right.Author) &&
               AreFootersEqual(left.Footer, right.Footer) &&
               AreFieldsEqual(left.Fields, right.Fields);
    }

    private static bool AreFieldsEqual(List<EmbedFieldBuilder> left, List<EmbedFieldBuilder> right)
    {
        if (left.Count != right.Count) return false;
        
        return left.Zip(right, (l, r) => 
            l.Name == r.Name && 
            l.Value?.ToString() == r.Value?.ToString() && 
            l.IsInline == r.IsInline
        ).All(equal => equal);
    }
}
```

### 4. 【重要度：中】WebhookクライアントのDI対応
**改善案**:
```csharp
/// <summary>
/// Discord Webhookクライアントのインターフェース
/// </summary>
public interface IDiscordWebhookClient : IDisposable
{
    Task<ulong> SendMessageAsync(Embed embed);
    Task UpdateMessageAsync(ulong messageId, Embed embed);
    bool IsConfigured { get; }
}

/// <summary>
/// Discord.NET Webhookクライアントの実装
/// </summary>
internal class DiscordNetWebhookClient : IDiscordWebhookClient
{
    private readonly DiscordWebhookClient? _client;
    private readonly ILogger<DiscordNetWebhookClient> _logger;

    public DiscordNetWebhookClient(string? webhookUrl, ILogger<DiscordNetWebhookClient> logger)
    {
        _logger = logger;
        
        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            try
            {
                _client = new DiscordWebhookClient(webhookUrl);
                IsConfigured = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Discord webhook client with URL: {Url}", webhookUrl);
                IsConfigured = false;
            }
        }
        else
        {
            IsConfigured = false;
        }
    }

    public bool IsConfigured { get; }

    public async Task<ulong> SendMessageAsync(Embed embed)
    {
        if (_client == null)
            throw new InvalidOperationException("Discord webhook is not configured");

        try
        {
            return await _client.SendMessageAsync(embeds: new[] { embed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord message");
            throw new DiscordNotificationException("Failed to send message to Discord", ex);
        }
    }

    public async Task UpdateMessageAsync(ulong messageId, Embed embed)
    {
        if (_client == null)
            throw new InvalidOperationException("Discord webhook is not configured");

        try
        {
            await _client.ModifyMessageAsync(messageId, msg => msg.Embeds = new[] { embed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Discord message {MessageId}", messageId);
            throw new DiscordNotificationException($"Failed to update message {messageId}", ex);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class DiscordNotificationException : Exception
{
    public DiscordNotificationException(string message) : base(message) { }
    public DiscordNotificationException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 5. 【重要度：低】キャッシュ機能の改善
**改善案**:
```csharp
/// <summary>
/// LRUキャッシュ付きの通知サービス
/// </summary>
internal class CachedDiscordNotificationService : IDiscordNotificationService
{
    private readonly IDiscordNotificationService _innerService;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public CachedDiscordNotificationService(
        IDiscordNotificationService innerService,
        IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 1
        };
    }

    public async Task<bool> IsEmbedChangedAsync(string joinId, Embed embed)
    {
        var cacheKey = $"embed_content_{joinId}";
        var lastEmbedHash = _cache.Get<string>(cacheKey);
        var currentEmbedHash = ComputeEmbedHash(embed);

        if (lastEmbedHash == currentEmbedHash)
            return false;

        _cache.Set(cacheKey, currentEmbedHash, _cacheOptions);
        return true;
    }

    private string ComputeEmbedHash(Embed embed)
    {
        var content = $"{embed.Title}|{embed.Description}|{string.Join("|", embed.Fields.Select(f => $"{f.Name}:{f.Value}"))}";
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
```

## 推奨されるNext Steps
1. DI対応とインターフェース抽出（高優先度）
2. リポジトリパターンの導入（高優先度）
3. エラーハンドリングの強化（中優先度）
4. キャッシュ機能の改善（中優先度）
5. 包括的な単体テストの追加（中優先度）

## コメント
Discord Webhook機能の基本的な実装は適切ですが、グローバル状態の多用とスレッドセーフティの欠如が深刻な問題です。依存関係注入とリポジトリパターンの導入により、よりテスタブルで保守しやすい設計にする必要があります。特に静的フィールドの排除とプロパーなライフサイクル管理の実装が急務です。