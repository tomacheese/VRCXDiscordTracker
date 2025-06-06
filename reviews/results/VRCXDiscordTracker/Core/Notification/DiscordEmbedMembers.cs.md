# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/Notification/DiscordEmbedMembers.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 431行（大規模）

## 概要
VRChatインスタンスのメンバー情報をDiscord Embed形式に変換する複雑なクラス。サイズ制限に応じた動的な表示調整機能を実装している。

## 総合評価
**スコア: 6/10**

高度な機能を実装しているが、複雑性、可読性、エラーハンドリングの観点で改善が必要。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐⭐☆☆
**Good:**
- 動的なサイズ調整機能
- 段階的なフォールバック機能
- 適切な責任分離

**Issues:**
- 過度に複雑なアルゴリズム
- 長いメソッド
- 多数の条件分岐

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐☆
**Good:**
- C#命名規約に準拠
- XMLドキュメンテーションが充実
- モダンなC#構文の使用

**Issues:**
- メソッドの長さ
- 複雑な条件式

### 3. セキュリティ上の問題 ⭐⭐⭐⭐☆
**Good:**
- 適切なエスケープ処理
- 入力検証

**Minor Issues:**
- URL生成時の検証不足

### 4. パフォーマンスの問題 ⭐⭐⭐☆☆
**Good:**
- 効率的な文字列処理
- 適切なコレクション使用

**Issues:**
- 繰り返し処理でのValidateEmbed呼び出し
- 文字列の重複操作

### 5. 可読性・保守性 ⭐⭐☆☆☆
**Issues:**
- 極めて複雑なロジック
- 長い条件分岐
- デバッグ用Console.WriteLineの多用

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- 複雑なロジックでテスト困難
- 外部依存（Discord.NET）
- 副作用の多い処理

## 具体的な問題点と改善提案

### 1. 【重要度：高】クラス分割と責任の明確化
**問題**: 単一クラスに複雑な処理が集中

**改善案**:
```csharp
/// <summary>
/// Discord Embed構築の責任を分離
/// </summary>
internal class DiscordEmbedBuilder
{
    private readonly IEmbedSizeCalculator _sizeCalculator;
    private readonly IEmbedFieldGenerator _fieldGenerator;
    private readonly IEmbedContentFormatter _contentFormatter;

    public DiscordEmbedBuilder(
        IEmbedSizeCalculator sizeCalculator,
        IEmbedFieldGenerator fieldGenerator,
        IEmbedContentFormatter contentFormatter)
    {
        _sizeCalculator = sizeCalculator;
        _fieldGenerator = fieldGenerator;
        _contentFormatter = contentFormatter;
    }

    public Embed BuildEmbed(MyLocation location, List<InstanceMember> members)
    {
        var baseEmbed = CreateBaseEmbed(location, members);
        var fields = _fieldGenerator.GenerateFields(members);
        var optimizedFields = _sizeCalculator.OptimizeFields(baseEmbed, fields);
        
        return baseEmbed.WithFields(optimizedFields).Build();
    }
}

/// <summary>
/// Embedサイズ計算とフィールド最適化
/// </summary>
internal class EmbedSizeCalculator : IEmbedSizeCalculator
{
    private readonly List<IFieldOptimizationStrategy> _strategies;

    public EmbedSizeCalculator()
    {
        _strategies = [
            new FullFormatStrategy(),
            new ExcludeLinksStrategy(),
            new NameOnlyStrategy(),
            new FieldReductionStrategy()
        ];
    }

    public List<EmbedFieldBuilder> OptimizeFields(EmbedBuilder baseEmbed, List<EmbedFieldBuilder> fields)
    {
        foreach (var strategy in _strategies)
        {
            var optimizedFields = strategy.Optimize(baseEmbed, fields);
            if (IsValidEmbed(baseEmbed.WithFields(optimizedFields)))
            {
                return optimizedFields;
            }
        }
        
        throw new EmbedOptimizationException("Unable to fit content within Discord limits");
    }
}

/// <summary>
/// フィールド最適化戦略のインターフェース
/// </summary>
internal interface IFieldOptimizationStrategy
{
    List<EmbedFieldBuilder> Optimize(EmbedBuilder baseEmbed, List<EmbedFieldBuilder> fields);
    int Priority { get; }
}

/// <summary>
/// フィールド数削減戦略
/// </summary>
internal class FieldReductionStrategy : IFieldOptimizationStrategy
{
    public int Priority => 100;

    public List<EmbedFieldBuilder> Optimize(EmbedBuilder baseEmbed, List<EmbedFieldBuilder> fields)
    {
        if (fields.Count <= 25) return fields;

        var reducedFields = fields.Take(25).ToList();
        
        // 最後のフィールドに "... and X more" を追加
        var omittedCount = fields.Count - 25;
        if (reducedFields.Any())
        {
            var lastField = reducedFields.Last();
            lastField.Value += $"\n... and {omittedCount} more";
        }

        return reducedFields;
    }
}
```

### 2. 【重要度：高】エラーハンドリングの改善
**問題**: 例外処理が不完全、適切なエラー型未使用

**改善案**:
```csharp
/// <summary>
/// Embed構築専用の例外クラス
/// </summary>
public class EmbedBuildException : Exception
{
    public EmbedBuildException(string message) : base(message) { }
    public EmbedBuildException(string message, Exception innerException) : base(message, innerException) { }
}

public class EmbedOptimizationException : EmbedBuildException
{
    public int AttemptedFieldCount { get; }
    public int ContentLength { get; }

    public EmbedOptimizationException(string message, int fieldCount, int contentLength) : base(message)
    {
        AttemptedFieldCount = fieldCount;
        ContentLength = contentLength;
    }
}

/// <summary>
/// 堅牢なEmbed構築
/// </summary>
public Embed BuildEmbedSafely(MyLocation location, List<InstanceMember> members)
{
    try
    {
        ValidateInputs(location, members);
        return BuildEmbed(location, members);
    }
    catch (FormatException ex)
    {
        throw new EmbedBuildException($"Invalid location format: {ex.Message}", ex);
    }
    catch (ArgumentException ex)
    {
        throw new EmbedBuildException($"Invalid member data: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
        throw new EmbedBuildException($"Unexpected error building embed: {ex.Message}", ex);
    }
}

private void ValidateInputs(MyLocation location, List<InstanceMember> members)
{
    ArgumentNullException.ThrowIfNull(location, nameof(location));
    ArgumentNullException.ThrowIfNull(members, nameof(members));

    if (string.IsNullOrWhiteSpace(location.WorldName))
        throw new ArgumentException("World name cannot be empty", nameof(location));

    if (string.IsNullOrWhiteSpace(location.LocationId))
        throw new ArgumentException("Location ID cannot be empty", nameof(location));

    if (!location.LocationId.Contains(':'))
        throw new FormatException("Location ID must contain a colon separator");
}
```

### 3. 【重要度：中】パフォーマンスの最適化
**改善案**:
```csharp
/// <summary>
/// キャッシュ機能付きのEmbed構築
/// </summary>
internal class CachedEmbedBuilder : IEmbedBuilder
{
    private readonly IEmbedBuilder _innerBuilder;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(1);

    public CachedEmbedBuilder(IEmbedBuilder innerBuilder, IMemoryCache cache)
    {
        _innerBuilder = innerBuilder;
        _cache = cache;
    }

    public Embed BuildEmbed(MyLocation location, List<InstanceMember> members)
    {
        var cacheKey = GenerateCacheKey(location, members);
        
        if (_cache.TryGetValue(cacheKey, out Embed cachedEmbed))
        {
            return UpdateTimestamp(cachedEmbed);
        }

        var embed = _innerBuilder.BuildEmbed(location, members);
        _cache.Set(cacheKey, embed, _cacheExpiry);
        return embed;
    }

    private string GenerateCacheKey(MyLocation location, List<InstanceMember> members)
    {
        var memberHash = string.Join(",", members
            .OrderBy(m => m.UserId)
            .Select(m => $"{m.UserId}:{m.IsCurrently}:{m.LastJoinAt?.Ticks}:{m.LastLeaveAt?.Ticks}"));
        
        return $"{location.JoinId}:{location.LocationId}:{memberHash.GetHashCode()}";
    }
}

/// <summary>
/// 効率的な文字列操作
/// </summary>
internal class EfficientStringBuilder
{
    private readonly StringBuilder _builder = new();

    public EfficientStringBuilder AppendMemberLine(InstanceMember member, MemberTextFormat format)
    {
        var emoji = GetMemberEmoji(member);
        var name = $"`{SanitizeForDiscord(member.DisplayName)}`";

        _builder.Append(emoji).Append(' ');

        switch (format)
        {
            case MemberTextFormat.Full:
                _builder.Append('[').Append(name).Append("](")
                        .Append("https://vrchat.com/home/user/").Append(member.UserId)
                        .Append("): ");
                AppendJoinLeaveTime(member);
                break;
            case MemberTextFormat.ExcludeLinks:
                _builder.Append(name).Append(": ");
                AppendJoinLeaveTime(member);
                break;
            case MemberTextFormat.NameOnly:
                _builder.Append(name);
                break;
        }

        return this;
    }

    public string Build() => _builder.ToString();
}
```

### 4. 【重要度：中】ログ機能の改善
**改善案**:
```csharp
/// <summary>
/// 構造化ログ対応
/// </summary>
internal class EmbedBuildLogger
{
    private readonly ILogger<DiscordEmbedMembers> _logger;

    public EmbedBuildLogger(ILogger<DiscordEmbedMembers> logger)
    {
        _logger = logger;
    }

    public void LogEmbedBuildStart(int memberCount)
    {
        _logger.LogInformation("Starting embed build for {MemberCount} members", memberCount);
    }

    public void LogOptimizationAttempt(string strategyName, int fieldCount, int contentLength)
    {
        _logger.LogDebug("Trying optimization strategy {Strategy} with {FieldCount} fields, {ContentLength} chars",
            strategyName, fieldCount, contentLength);
    }

    public void LogOptimizationSuccess(string strategyName)
    {
        _logger.LogInformation("Embed optimization succeeded with strategy {Strategy}", strategyName);
    }

    public void LogOptimizationFailure(string error)
    {
        _logger.LogWarning("Embed optimization failed: {Error}", error);
    }
}
```

### 5. 【重要度：低】設定の外部化
**改善案**:
```csharp
/// <summary>
/// Embed構築設定
/// </summary>
public class EmbedBuildSettings
{
    public int MaxFieldCount { get; set; } = 25;
    public int MaxFieldValueLength { get; set; } = 1024;
    public int MaxEmbedLength { get; set; } = 6000;
    public bool EnableFieldReduction { get; set; } = true;
    public bool EnableContentTruncation { get; set; } = true;
    public string TruncationSuffix { get; set; } = "...";
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// 絵文字設定
/// </summary>
public class EmojiSettings
{
    public string InstanceOwner { get; set; } = "👑";
    public string Self { get; set; } = "👤";
    public string Friend { get; set; } = "⭐️";
    public string Other { get; set; } = "⬜️";
}
```

## 推奨されるNext Steps
1. クラス分割と責任の明確化（高優先度）
2. エラーハンドリングの強化（高優先度）
3. デバッグ用ログの適切な実装（中優先度）
4. パフォーマンス最適化（中優先度）
5. 包括的な単体テストの追加（中優先度）

## コメント
高度で複雑な機能を実装しており、Discord APIの制限に対する対応は評価できます。しかし、430行を超える巨大なクラスは保守性に深刻な問題があります。特に`GetEmbed`メソッドの複雑さとデバッグ用Console.WriteLineの多用は、プロダクションコードとしては不適切です。クラス分割と適切なログ機能の実装により、より保守しやすい設計にする必要があります。