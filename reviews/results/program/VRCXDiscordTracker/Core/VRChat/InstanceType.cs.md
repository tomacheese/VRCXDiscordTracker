# InstanceType.cs レビュー

## 概要

`InstanceType.cs`はVRChatのインスタンスタイプを表現する列挙型に似た値オブジェクトを定義しています。Public、Friends、Invite、Groupなどの様々なインスタンスタイプを静的インスタンスとして提供し、比較や表示のためのメソッドを実装しています。

## 良い点

1. プリミティブな列挙型ではなく、完全なクラスとして実装されており、拡張性が高い
2. 静的インスタンスを使用して、異なるインスタンスタイプを明確に定義している
3. `IComparable<T>`、`IEquatable<T>`、`IFormattable`インターフェースを実装し、整列、比較、文字列フォーマットをサポートしている
4. リフレクションを使用して、すべての定義されたインスタンスタイプを取得する機能を提供している

## 改善点

### 1. シールドクラスの検討

このクラスは継承される意図がないため、`sealed`修飾子を追加することで、意図を明確にし、パフォーマンスの最適化の余地を提供できます。

```csharp
/// <summary>
/// VRChatのインスタンスの種類を表すクラス
/// </summary>
/// <param name="id">管理用ID</param>
/// <param name="name">表示名</param>
internal sealed class InstanceType(int id, string name) : IComparable<InstanceType>, IEquatable<InstanceType>, IFormattable
{
    // 既存のコード
}
```

### 2. 静的コンストラクタを使用した初期化

現在、静的フィールドは直接初期化されていますが、静的コンストラクタを使用することで、より複雑な初期化ロジックに対応できます。

```csharp
/// <summary>
/// すべてのインスタンスタイプを格納するリスト
/// </summary>
private static readonly List<InstanceType> _allTypes = new();

/// <summary>
/// 静的コンストラクタ
/// </summary>
static InstanceType()
{
    Public = new(0, "Public");
    FriendsPlus = new(1, "Friends+");
    Friends = new(2, "Friends");
    InvitePlus = new(3, "Invite+");
    Invite = new(4, "Invite");
    GroupPublic = new(5, "Group Public");
    GroupPlus = new(6, "Group+");
    Group = new(7, "Group");
    
    // すべてのインスタンスをリストに追加
    _allTypes.AddRange(new[] { Public, FriendsPlus, Friends, InvitePlus, Invite, GroupPublic, GroupPlus, Group });
}

/// <summary>
/// すべての定義されたインスタンスタイプを取得する
/// </summary>
/// <returns>すべてのインスタンスタイプのコレクション</returns>
public static IReadOnlyCollection<InstanceType> GetAll() => _allTypes.AsReadOnly();
```

### 3. GetAll<T>メソッドの最適化

現在の`GetAll<T>`メソッドはリフレクションを使用していますが、これはパフォーマンス上のオーバーヘッドがあります。静的なリストを使用することで、パフォーマンスを向上させることができます。

```csharp
/// <summary>
/// すべての定義されたインスタンスタイプを取得する
/// </summary>
/// <returns>すべてのインスタンスタイプのコレクション</returns>
public static IEnumerable<InstanceType> GetAll() => _allTypes;
```

### 4. 値オブジェクトの不変性の強化

値オブジェクトは不変であるべきです。現在の実装では、プロパティがpublicとして公開されていますが、読み取り専用のプロパティにすることで不変性を強化できます。

```csharp
/// <summary>
/// 管理用ID
/// </summary>
public int Id { get; }

/// <summary>
/// 表示名
/// </summary>
/// <example>Public</example>
public string Name { get; }

public InstanceType(int id, string name)
{
    Id = id;
    Name = name;
}
```

### 5. IDによるインスタンス取得メソッドの追加

`GetByToken`メソッドがある`InstanceRegion`クラスと同様に、IDによるインスタンス取得メソッドを追加すると便利です。

```csharp
/// <summary>
/// 指定されたIDに対応するインスタンスタイプを取得する
/// </summary>
/// <param name="id">インスタンスタイプのID</param>
/// <returns>対応するインスタンスタイプ、または存在しない場合はnull</returns>
public static InstanceType? GetById(int id) => _allTypes.FirstOrDefault(t => t.Id == id);

/// <summary>
/// 指定された名前に対応するインスタンスタイプを取得する
/// </summary>
/// <param name="name">インスタンスタイプの名前</param>
/// <returns>対応するインスタンスタイプ、または存在しない場合はnull</returns>
public static InstanceType? GetByName(string name) => 
    _allTypes.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
```

### 6. ToStringメソッドのオーバーライド

`IFormattable.ToString`は実装されていますが、通常の`ToString()`メソッドがオーバーライドされていません。

```csharp
/// <summary>
/// オブジェクトの文字列表現を取得する
/// </summary>
/// <returns>インスタンスタイプの名前</returns>
public override string ToString() => Name;
```

### 7. パースメソッドの追加

文字列からインスタンスタイプを解析するメソッドを追加すると、シリアライズ/デシリアライズが容易になります。

```csharp
/// <summary>
/// 文字列からインスタンスタイプを解析する
/// </summary>
/// <param name="value">解析する文字列</param>
/// <returns>対応するインスタンスタイプ</returns>
/// <exception cref="ArgumentException">対応するインスタンスタイプが見つからない場合</exception>
public static InstanceType Parse(string value)
{
    // 名前による検索
    var type = GetByName(value);
    if (type != null)
        return type;
        
    // ID（数値）による検索
    if (int.TryParse(value, out int id))
    {
        type = GetById(id);
        if (type != null)
            return type;
    }
    
    throw new ArgumentException($"Unknown instance type: {value}");
}

/// <summary>
/// 文字列からインスタンスタイプの解析を試みる
/// </summary>
/// <param name="value">解析する文字列</param>
/// <param name="result">解析結果を格納する変数</param>
/// <returns>解析に成功した場合はtrue、それ以外はfalse</returns>
public static bool TryParse(string value, out InstanceType? result)
{
    result = null;
    
    // 名前による検索
    var type = GetByName(value);
    if (type != null)
    {
        result = type;
        return true;
    }
    
    // ID（数値）による検索
    if (int.TryParse(value, out int id))
    {
        type = GetById(id);
        if (type != null)
        {
            result = type;
            return true;
        }
    }
    
    return false;
}
```

## セキュリティ上の懸念点

特にセキュリティ上の懸念は見当たりませんが、値オブジェクトの不変性を確保することで、予期しない変更を防ぐことができます。

## 総合評価

`InstanceType`クラスは値オブジェクトパターンを効果的に実装しており、VRChatのインスタンスタイプを表現するための堅牢な基盤を提供しています。提案した改善点を適用することで、不変性の強化、パフォーマンスの向上、使いやすさの向上が期待できます。特に、静的初期化の最適化とID/名前によるインスタンス取得メソッドの追加は、クラスの使用性を大幅に向上させるでしょう。
