# コードレビュー: VRCXDiscordTracker/Core/Notification/DiscordEmbedMembers.cs

## 概要

このクラスはVRChatインスタンスとそのメンバー情報を取得し、Discordに表示するためのEmbedオブジェクトを生成します。現在インスタンスにいるメンバーと過去に訪れたメンバーを区別し、文字数制限に対応するための複数の表示フォーマットパターンを実装しています。

## 良い点

- プライマリコンストラクタを使用して依存オブジェクトを注入しています。
- 正規表現に[GeneratedRegex]属性を使用して、コンパイル時に正規表現を生成しています。
- Discordの文字数制限に対応するためのいくつかの表示パターンを実装し、状況に応じて最適なパターンを選択しています。
- XMLドキュメントコメントが適切に記述されており、メソッドの目的と使用方法が明確です。

## 改善点

### 1. コードの複雑さ

```csharp
// パターン選択のためのループが複雑で理解しにくいです
foreach (EmbedFieldPattern pattern in patterns)
{
    Console.WriteLine($"Trying pattern with Current: {pattern.CurrentFormat}, Past: {pattern.PastFormat}");
    List<EmbedFieldBuilder> currentFields = GetMemberFields(MemberStatus.Currently, currentMembers, pattern.CurrentFormat);
    List<EmbedFieldBuilder> pastFields = GetMemberFields(MemberStatus.Past, pastMembers, pattern.PastFormat);
    var combinedFields = currentFields.Concat(pastFields).ToList();
    if (pattern.IsReducible)
    {
        combinedFields = ReduceFields(baseEmbed, combinedFields);
    }

    EmbedBuilder patternEmbed = SetFields(baseEmbed, combinedFields);
    if (ValidateEmbed(patternEmbed))
    {
        Console.WriteLine($"Selected build pattern with Current: {pattern.CurrentFormat}, Past: {pattern.PastFormat}");
        return patternEmbed.Build();
    }
}

// パターン選択のロジックをより明確にし、Strategy パターンを検討するべきです
public interface IEmbedFormatStrategy
{
    bool TryFormat(EmbedBuilder baseEmbed, List<InstanceMember> currentMembers, 
                  List<InstanceMember> pastMembers, out EmbedBuilder result);
}

// 実装例
public class FullFormatStrategy : IEmbedFormatStrategy
{
    public bool TryFormat(EmbedBuilder baseEmbed, List<InstanceMember> currentMembers, 
                         List<InstanceMember> pastMembers, out EmbedBuilder result)
    {
        // フルフォーマットで試行する実装
        // ...
    }
}

// 使用例
foreach (var strategy in _formatStrategies)
{
    if (strategy.TryFormat(baseEmbed, currentMembers, pastMembers, out var formattedEmbed))
    {
        return formattedEmbed.Build();
    }
}
```

### 2. デバッグ出力の管理

```csharp
// コンソール出力が全体に散在しています
Console.WriteLine($"GetEmbed started. Total members: {instanceMembers.Count}");
Console.WriteLine($"CurrentMembers count: {currentMembers.Count}");
Console.WriteLine($"PastMembers count: {pastMembers.Count}");
Console.WriteLine($"Trying pattern with Current: {pattern.CurrentFormat}, Past: {pattern.PastFormat}");
Console.WriteLine($"Selected build pattern with Current: {pattern.CurrentFormat}, Past: {pattern.PastFormat}");
Console.WriteLine($"GetBaseEmbed - World: {myLocation.WorldName}, Type: {myLocation.LocationInstance.Type}");

// ロギングシステムを使用し、適切なログレベルでログ出力するべきです
private readonly ILogger _logger;

public DiscordEmbedMembers(MyLocation myLocation, List<InstanceMember> instanceMembers, ILogger logger)
{
    this.myLocation = myLocation;
    this.instanceMembers = instanceMembers;
    _logger = logger;
}

// 使用例
_logger.Debug($"GetEmbed started. Total members: {instanceMembers.Count}");
_logger.Info($"Selected build pattern with Current: {pattern.CurrentFormat}, Past: {pattern.PastFormat}");
```

### 3. エラー処理

```csharp
// パターン選択が全て失敗した場合に例外をスローしています
throw new Exception("Failed to build a valid embed with the given patterns.");

// 代わりにフォールバックパターンを用意するべきです
// すべてのパターンが失敗した場合の最後の手段として
EmbedBuilder FallbackEmbed()
{
    var fallback = baseEmbed.Clone();
    fallback.AddField("Members", "Member list is too long to display. Please check in-game.");
    return fallback;
}

// 使用例
foreach (var pattern in patterns)
{
    // パターン試行...
}
// すべてのパターンが失敗した場合
return FallbackEmbed().Build();
```

### 4. リファクタリングのため内部クラスの分離

```csharp
// EmbedFieldPattern クラスが内部で定義されています（コード一部省略）
EmbedFieldPattern[] patterns = [
    new EmbedFieldPattern { CurrentFormat = MemberTextFormat.Full, PastFormat = MemberTextFormat.Full, IsReducible = false },
    // ...
];

// 複雑な内部クラスやデータ構造は別ファイルに分離するべきです
// EmbedFieldPattern.cs として分離
namespace VRCXDiscordTracker.Core.Notification
{
    /// <summary>
    /// Discord Embedフィールドの表示パターン設定
    /// </summary>
    public class EmbedFieldPattern
    {
        public MemberTextFormat CurrentFormat { get; set; }
        public MemberTextFormat PastFormat { get; set; }
        public bool IsReducible { get; set; }
    }
}
```

### 5. テスト容易性の向上

```csharp
// 一部のメソッドがprivateで、テストが困難です
private EmbedBuilder GetBaseEmbed()
{
    // ...
}

private List<EmbedFieldBuilder> GetMemberFields(MemberStatus status, List<InstanceMember> members, MemberTextFormat format)
{
    // ...
}

// internal にして、InternalsVisibleToを使用してテストを可能にするべきです
// AssemblyInfo.cs に次の行を追加
[assembly: InternalsVisibleTo("VRCXDiscordTracker.Tests")]

// メソッドをinternalに変更
internal EmbedBuilder GetBaseEmbed()
{
    // ...
}
```

## セキュリティの問題

- ユーザー名やインスタンス名などがDiscordのMarkdown構文に影響する可能性がありますが、特定の文字（アンダースコア）のエスケープのみが実装されています。より堅牢なMarkdownエスケープ処理を検討するべきです。
- URLが直接フォーマットされていますが、特殊文字が含まれる場合はエンコードする必要があります。

## パフォーマンスの問題

- パターン選択のループで、各パターンごとにフィールドを生成して検証しています。これは効率的ではなく、大量のメンバーがいる場合はパフォーマンスに影響する可能性があります。表示サイズの事前計算など、最適化を検討すべきです。

## テスト容易性

- プライベートメソッドが多く、単体テストが難しい設計になっています。メソッドの可視性を見直し、テスト可能な設計にすることを検討してください。
- 文字数制限のチェックロジックは複雑であり、このロジックをテストするための専用のテストケースが必要です。

## その他のコメント

- Embed生成ロジックとフォーマットロジックが分離されていません。テンプレートパターンを使用して、基本構造と具体的なフォーマットロジックを分離することを検討してください。
- クラスが長く複雑になっていますが、責務の分離ができていない可能性があります。単一責任の原則に従って、クラスをより小さく、焦点を絞ったクラスに分解することを検討してください。
