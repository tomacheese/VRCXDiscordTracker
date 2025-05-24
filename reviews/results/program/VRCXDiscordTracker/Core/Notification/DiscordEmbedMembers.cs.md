# DiscordEmbedMembers.cs レビュー

## 概要

`DiscordEmbedMembers.cs`はVRChatのインスタンス情報とメンバー情報をDiscordのEmbed形式に変換するクラスです。ユーザーの位置情報とインスタンス内のメンバーリストを受け取り、様々な表示形式でDiscord向けの埋め込みメッセージを生成します。

## 良い点

1. 詳細なXMLドキュメントコメントが記述されており、メソッドやプロパティの役割が明確
2. 表示形式を複数用意し、文字数制限を超えた場合のフォールバック処理が実装されている
3. レコード型や列挙型を適切に使用して、関連するデータをまとめている
4. 正規表現を使った特殊文字のエスケープ処理が実装されている
5. パターンベースの設計により、表示形式の選択ロジックが柔軟

## 改善点

### 1. クラス構造の見直し

現在、すべてのロジックが1つのクラスに集約されています。責務をより明確に分離するために、クラスの分割を検討すべきです。

```csharp
// 外部向けの主要クラス
internal class DiscordEmbedBuilder
{
    private readonly InstanceData _instanceData;
    private readonly MemberFormatter _memberFormatter;
    
    public DiscordEmbedBuilder(MyLocation myLocation, List<InstanceMember> instanceMembers)
    {
        _instanceData = new InstanceData(myLocation, instanceMembers);
        _memberFormatter = new MemberFormatter();
    }
    
    public Embed BuildEmbed()
    {
        // 既存のGetEmbed()ロジックをここに移動
    }
}

// インスタンス情報を扱うクラス
internal class InstanceData
{
    public MyLocation MyLocation { get; }
    public List<InstanceMember> Members { get; }
    
    public InstanceData(MyLocation myLocation, List<InstanceMember> instanceMembers)
    {
        MyLocation = myLocation;
        Members = instanceMembers;
    }
    
    // インスタンス情報に関連するメソッド
}

// メンバー表示形式を扱うクラス
internal class MemberFormatter
{
    // メンバー表示に関連するメソッド
}
```

### 2. 正規表現の最適化

正規表現が非常に複雑で理解しにくくなっています。また、部分式として分割することでメンテナンス性が向上します。

```csharp
/// <summary>
/// 連続するアンダースコアをエスケープする Regex を生成する
/// </summary>
[GeneratedRegex(@"(?<!<a?:.+|https?:\/\/\S+)__(_)?(?!:\d+>)")]
private static partial Regex SanitizeUnderscoreRegex();
```

改善案:

```csharp
/// <summary>
/// Discord向けテキスト処理を行うクラス
/// </summary>
internal static class DiscordTextFormatter
{
    /// <summary>
    /// 絵文字・URL以外の場所に現れる連続アンダースコアをエスケープする正規表現
    /// </summary>
    [GeneratedRegex(@"(?<!<a?:.+|https?:\/\/\S+)__(_)?(?!:\d+>)")]
    private static partial Regex SanitizeUnderscoreRegex();
    
    /// <summary>
    /// テキストをDiscord用にサニタイズする
    /// </summary>
    public static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // アンダースコアのエスケープ
        return SanitizeUnderscoreRegex().Replace(text, @"\$0");
    }
}
```

### 3. マジックナンバーの排除

コード内に埋め込まれた数値定数（マジックナンバー）を定数や設定として外部化すべきです。

```csharp
private const int MAX_EMBED_LENGTH = 6000;
private const int MAX_FIELD_VALUE_LENGTH = 1024;
private const int MAX_FIELD_NAME_LENGTH = 256;
private const int MAX_CURRENT_MEMBERS_DISPLAY = 25;
private const int MAX_PAST_MEMBERS_DISPLAY = 25;
```

### 4. 例外処理の強化

現在、一部のメソッドで例外がスローされていますが、それらのハンドリングが上位のコードで行われていません。また、例外メッセージが英語のままです。

```csharp
private EmbedBuilder GetBaseEmbed()
{
    try
    {
        // 既存コード...
        
        var locationParts = myLocation.LocationId.Split(':');
        if (locationParts.Length != 2)
        {
            throw new FormatException("ロケーション文字列がコロンを含む期待された形式ではありません。");
        }
        
        // 残りのコード...
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Embedの基本情報作成中にエラーが発生しました: {ex.Message}");
        // フォールバックEmbed生成
        return CreateFallbackEmbed();
    }
}

private EmbedBuilder CreateFallbackEmbed()
{
    return new EmbedBuilder()
    {
        Title = "インスタンス情報（エラー）",
        Description = "インスタンス情報の読み取り中にエラーが発生しました。",
        Color = Color.Red
    };
}
```

### 5. パフォーマンスの最適化

複数のLINQクエリが繰り返し実行されており、パフォーマンスに影響を与える可能性があります。結果をキャッシュすることで改善できます。

```csharp
// 改善前:
$"Current Members Count: {instanceMembers.Count(member => member.IsCurrently)}\n" +
$"Past Members Count: {instanceMembers.Count(member => !member.IsCurrently)}\n"

// 改善後:
private (int CurrentCount, int PastCount) GetMemberCounts()
{
    int currentCount = 0;
    int pastCount = 0;
    
    foreach (var member in instanceMembers)
    {
        if (member.IsCurrently)
            currentCount++;
        else
            pastCount++;
    }
    
    return (currentCount, pastCount);
}

// 使用例:
var counts = GetMemberCounts();
$"Current Members Count: {counts.CurrentCount}\n" +
$"Past Members Count: {counts.PastCount}\n"
```

### 6. 設定の外部化

表示形式や文字数制限などの設定がクラス内部にハードコードされています。これを外部設定として分離すべきです。

```csharp
/// <summary>
/// Discord Embedの設定
/// </summary>
internal class DiscordEmbedConfig
{
    /// <summary>
    /// フィールド表示形式のパターン
    /// </summary>
    public static EmbedFieldPattern[] DefaultPatterns => [
        new() { CurrentFormat = MemberTextFormat.Full, PastFormat = MemberTextFormat.Full, IsReducible = false },
        new() { CurrentFormat = MemberTextFormat.Full, PastFormat = MemberTextFormat.ExcludeLinks, IsReducible = false },
        // 他のパターン...
    ];
    
    // 他の設定...
}
```

### 7. インターフェースの導入

テスト容易性や拡張性を高めるために、インターフェースを導入すべきです。

```csharp
/// <summary>
/// Discord Embed生成のインターフェース
/// </summary>
internal interface IDiscordEmbedBuilder
{
    /// <summary>
    /// Discordの埋め込みメッセージを生成する
    /// </summary>
    Embed BuildEmbed();
}

/// <summary>
/// インスタンスメンバー情報からDiscord Embedを生成するクラス
/// </summary>
internal class DiscordEmbedMembers : IDiscordEmbedBuilder
{
    // 既存の実装...
    
    /// <summary>
    /// インスタンスメンバー情報からDiscord Embedを構築する
    /// </summary>
    public Embed BuildEmbed()
    {
        // 既存のGetEmbed()実装
    }
}
```

### 8. 不必要な改行の修正

一部のレコード定義やクラス定義に不要な改行が含まれています。

```diff
- private record EmbedFieldPattern
- {
+ private record EmbedFieldPattern {
    // プロパティ...
}
```

## セキュリティ上の懸念点

特に大きなセキュリティ上の懸念は見られませんが、以下の点には注意が必要です：

1. ユーザー入力データ（表示名など）のサニタイズは行われているが、攻撃ベクトルを完全に排除しているわけではない
2. ワールド名やユーザー名に機密情報が含まれている場合、Discordへの送信によって情報が漏洩する可能性がある
3. VRChat APIのURLが直接埋め込まれており、APIの変更に追従する必要がある

## 総合評価

`DiscordEmbedMembers`クラスは基本的な機能を果たしていますが、責務の分離、パフォーマンスの最適化、設定の外部化、例外処理の強化などの面で改善の余地があります。特に、クラスの責務が大きくなりすぎていることが主な懸念点です。

レコード型や列挙型を適切に使用している点は評価できますが、より明確な構造にすることで、コードの保守性と拡張性が向上するでしょう。また、表示形式やパターンの設定を外部化することで、将来的な変更に対する柔軟性も高まります。
