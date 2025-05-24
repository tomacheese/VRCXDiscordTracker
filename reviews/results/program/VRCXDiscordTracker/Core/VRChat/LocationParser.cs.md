# LocationParser.cs レビュー

## 概要

`LocationParser.cs`はVRChatのロケーションID（インスタンスID）を解析し、その構成要素を抽出してVRChatInstanceオブジェクトを生成するためのユーティリティクラスです。正規表現を使用してロケーションIDの各部分を解析し、インスタンスタイプや地域などの情報を取得します。

## 良い点

1. 正規表現を使って複雑なロケーションIDを効率的に解析している
2. 例外を適切に使用して、不正な入力を処理している
3. コードが整理され、メソッドの責務が明確に分離されている
4. GeneratedRegexアトリビュートを使用して、正規表現のコンパイル時最適化を行っている（C# 11の機能）
5. 内部ロジックを隠蔽するために、ExtractedTokensを内部クラスとして実装している

## 改善点

### 1. 静的クラスの明示的宣言

クラスにインスタンスを作成する意図がなく、すべてのメソッドが静的である場合は、静的クラスとして明示的に宣言すべきです。

```csharp
/// <summary>
/// VRChatのロケーションIDを解析するクラス
/// </summary>
internal static partial class LocationParser
{
    // 既存のコード
}
```

### 2. 例外メッセージの多言語対応

現在の例外メッセージは英語ハードコードされていますが、リソースファイルを使用して多言語対応することを検討できます。

```csharp
throw new ArgumentException(Resources.LocationIdCannotBeNullOrEmpty, nameof(locationId));
```

### 3. 例外の種類の改善

現在の実装では、`FormatException`と`ArgumentException`が混在しています。VRChatロケーション解析用のカスタム例外クラスを作成することで、より明確なエラーハンドリングが可能になります。

```csharp
/// <summary>
/// VRChatのロケーションID解析に関連する例外
/// </summary>
public class LocationParseException : Exception
{
    public LocationParseException(string message) : base(message) { }
    public LocationParseException(string message, Exception innerException) : base(message, innerException) { }
}

// 使用例
if (string.IsNullOrWhiteSpace(locationId))
    throw new ArgumentException("Location ID cannot be null or empty.", nameof(locationId));

if (locationId.StartsWith("local:"))
    throw new LocationParseException("Local instances are not supported.");
```

### 4. ExtractedTokensクラスの改善

`ExtractedTokens`クラスのプロパティに`required`修飾子が使用されていますが、初期化子で値を設定しているため、必ずしも必要ではありません。また、現在のコードではコンストラクタが明示的に呼び出されていないため、ファクトリーメソッドパターンを使用することも検討できます。

```csharp
private class ExtractedTokens
{
    // required修飾子を削除
    public InstanceRegion? Region { get; set; }
    public string? GroupId { get; set; }
    public string? GroupAccessType { get; set; }
    public bool CanRequestInvite { get; set; }
    public string? CreatorId { get; set; }
    public bool IsHiddenToken { get; set; }
    public bool IsFriendsToken { get; set; }
    public bool IsInviteToken { get; set; }
    public bool IsGroupToken { get; set; }
    public string? Nonce { get; set; }

    // プライベートコンストラクタで、ファクトリーメソッドパターンを強制
    private ExtractedTokens() { }

    /// <summary>
    /// トークンを解析して、ExtractedTokensオブジェクトを生成する
    /// </summary>
    /// <param name="tokens">トークンの配列</param>
    /// <returns>ExtractedTokensオブジェクト</returns>
    public static ExtractedTokens Parse(string[] tokens)
    {
        var instance = new ExtractedTokens();
        
        // リストから最初の一致を探す代わりにLINQで処理
        var regionToken = tokens
            .Where(t => t.StartsWith("region("))
            .Select(t => t[7..^1])
            .FirstOrDefault();
            
        var creatorIdToken = tokens.FirstOrDefault(t => UserRegex().IsMatch(t));
        Match creatorIdMatch = UserRegex().Match(creatorIdToken ?? string.Empty);
        var creatorId = creatorIdMatch.Success ? creatorIdMatch.Groups["userId"].Value : null;
        
        instance.Region = InstanceRegion.GetByToken(regionToken);
        instance.GroupId = tokens
            .Where(t => t.StartsWith("group("))
            .Select(t => t[6..^1])
            .FirstOrDefault();
        instance.GroupAccessType = tokens
            .Where(t => t.StartsWith("groupAccessType("))
            .Select(t => t[16..^1])
            .FirstOrDefault();
        instance.CanRequestInvite = tokens.Contains("canRequestInvite");
        instance.CreatorId = creatorId;
        instance.IsHiddenToken = tokens.Any(t => t.StartsWith("hidden("));
        instance.IsFriendsToken = tokens.Any(t => t.StartsWith("friends("));
        instance.IsInviteToken = tokens.Any(t => t.StartsWith("private("));
        instance.IsGroupToken = tokens.Any(t => t.StartsWith("group("));
        instance.Nonce = tokens
            .Where(t => t.StartsWith("nonce("))
            .Select(t => t[6..^1])
            .FirstOrDefault();
            
        return instance;
    }
}
```

### 5. インスタンスタイプの決定ロジックの改善

現在の実装では、インスタンスタイプの決定ロジックが複雑なif-elseステートメントで構成されています。これをより宣言的なアプローチで実装することで、読みやすさと保守性が向上します。

```csharp
private static InstanceType GetInstanceType(ExtractedTokens extractedTokens)
{
    // グループインスタンスの処理
    if (extractedTokens.IsGroupToken)
    {
        return extractedTokens.GroupAccessType switch
        {
            "members" => InstanceType.Group,
            "plus" => InstanceType.GroupPlus,
            "public" => InstanceType.GroupPublic,
            _ => InstanceType.Group // デフォルト
        };
    }

    // フレンド/招待インスタンスの処理
    if (extractedTokens.IsHiddenToken)
        return InstanceType.FriendsPlus;
        
    if (extractedTokens.IsFriendsToken)
        return InstanceType.Friends;
        
    if (extractedTokens.IsInviteToken)
        return extractedTokens.CanRequestInvite ? InstanceType.InvitePlus : InstanceType.Invite;
        
    // デフォルト
    return InstanceType.Public;
}
```

### 6. null安全性の強化

現在のコードでは、nullチェックが適切に行われていますが、nullに対する安全性をさらに高めるために`??`や`?.`演算子を活用できます。

```csharp
Match creatorIdMatch = UserRegex().Match(creatorIdToken ?? string.Empty);
var creatorId = creatorIdMatch.Success ? creatorIdMatch.Groups["userId"]?.Value : null;
```

## セキュリティ上の懸念点

正規表現の処理は、不適切な入力パターンによりパフォーマンス問題（正規表現DoS）を引き起こす可能性があります。現在の実装では、GeneratedRegexアトリビュートを使用してコンパイル時に正規表現を最適化していますが、入力の長さに制限を設けることも検討すべきです。

```csharp
public static VRChatInstance Parse(string locationId)
{
    if (string.IsNullOrWhiteSpace(locationId))
        throw new ArgumentException("Location ID cannot be null or empty.", nameof(locationId));
        
    // 長すぎる入力を拒否
    if (locationId.Length > 1000) // 適切な上限値を設定
        throw new ArgumentException("Location ID is too long.", nameof(locationId));
        
    // 残りの処理...
}
```

## 総合評価

`LocationParser`クラスは、複雑なVRChatロケーションIDを解析するための効果的な実装を提供しています。コードは構造化されており、責務が明確に分離されています。ただし、静的クラスの明示的宣言、例外処理の改善、クラス設計の最適化などにより、さらに読みやすく保守しやすいコードになるでしょう。また、セキュリティとパフォーマンスの観点から、入力データに対する検証を強化することも重要です。
