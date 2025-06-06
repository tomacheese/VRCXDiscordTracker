# VRChatInstance.cs レビュー結果

## 概要
VRChatInstance.csは、VRChatのインスタンス情報を格納するデータモデルクラスです。ワールドID、インスタンス名、タイプ、所有者、地域、ナンスなどの基本的なインスタンス情報を保持します。

## コード品質評価

### 良い点
1. **明確なデータモデル**: VRChatインスタンスの核となる情報を適切に定義
2. **XMLドキュメント**: 各プロパティにサンプル値を含む詳細な説明
3. **required修飾子**: 必須プロパティの明確化
4. **適切な型使用**: enum型での型安全性確保

### 懸念事項・改善提案

#### 1. **重要**: ID検証の強化
```csharp
// 現在: 単純な文字列プロパティ
public required string WorldId { get; set; }

// 推奨: 検証付きプロパティ
public class VRChatInstance
{
    private string _worldId = string.Empty;
    private string _instanceName = string.Empty;

    /// <summary>
    /// ワールドID
    /// </summary>
    /// <example>wrld_12345678-1234-1234-1234-123456789abc</example>
    public required string WorldId 
    { 
        get => _worldId;
        set 
        {
            if (!IsValidWorldId(value))
                throw new ArgumentException($"Invalid WorldId format: {value}", nameof(value));
            _worldId = value;
        }
    }

    /// <summary>
    /// インスタンス名の検証
    /// </summary>
    public required string InstanceName 
    { 
        get => _instanceName;
        set 
        {
            if (!IsValidInstanceName(value))
                throw new ArgumentException($"Invalid InstanceName format: {value}", nameof(value));
            _instanceName = value;
        }
    }

    private static bool IsValidWorldId(string worldId)
    {
        return !string.IsNullOrWhiteSpace(worldId) && 
               worldId.StartsWith("wrld_") && 
               worldId.Length >= 41; // wrld_ + 36文字のUUID
    }

    private static bool IsValidInstanceName(string instanceName)
    {
        return !string.IsNullOrWhiteSpace(instanceName) && 
               instanceName.Length <= 64 && // 合理的な上限
               instanceName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }
}
```

#### 2. 所有者情報の型安全性向上
```csharp
// 現在: 文字列での所有者ID
public string? OwnerId { get; set; }

// 推奨: 所有者タイプの明確化
public class VRChatInstance
{
    public OwnerInfo? Owner { get; set; }
}

public class OwnerInfo
{
    public required string Id { get; set; }
    public required OwnerType Type { get; set; }
    
    public bool IsUser => Type == OwnerType.User;
    public bool IsGroup => Type == OwnerType.Group;
}

public enum OwnerType
{
    User,   // usr_xxx
    Group   // grp_xxx
}

// ファクトリーメソッド
public static class OwnerInfoFactory
{
    public static OwnerInfo? Create(string? ownerId)
    {
        if (string.IsNullOrEmpty(ownerId))
            return null;
            
        var type = ownerId.StartsWith("usr_") ? OwnerType.User :
                   ownerId.StartsWith("grp_") ? OwnerType.Group :
                   throw new ArgumentException($"Unknown owner ID format: {ownerId}");
                   
        return new OwnerInfo { Id = ownerId, Type = type };
    }
}
```

#### 3. イミュータブル設計の採用
```csharp
// 推奨: レコード型での実装
public record VRChatInstance
{
    /// <summary>
    /// ワールドID
    /// </summary>
    /// <example>wrld_12345678-1234-1234-1234-123456789abc</example>
    public required string WorldId { get; init; }

    /// <summary>
    /// インスタンス名。通常は5桁の数字だが、任意の英数字文字列にすることも可能
    /// </summary>
    /// <example>12345</example>
    public required string InstanceName { get; init; }

    /// <summary>
    /// インスタンスタイプ
    /// </summary>
    /// <example>InstanceType.Friends</example>
    public required InstanceType Type { get; init; }

    /// <summary>
    /// インスタンスの所有者情報
    /// </summary>
    public OwnerInfo? Owner { get; init; }

    /// <summary>
    /// インスタンスの地域
    /// </summary>
    /// <example>InstanceRegion.USWest</example>
    public required InstanceRegion Region { get; init; }

    /// <summary>
    /// ナンス
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789abc</example>
    public string? Nonce { get; init; }

    // 計算プロパティ
    public string FullInstanceId => $"{WorldId}:{InstanceName}";
    
    public bool IsPublic => Type == InstanceType.Public;
    public bool IsPrivate => Type is InstanceType.Invite or InstanceType.InvitePlus;
    public bool IsFriends => Type is InstanceType.Friends or InstanceType.FriendsPlus;
    public bool IsGroup => Type is InstanceType.Group or InstanceType.GroupPlus or InstanceType.GroupPublic;
}
```

#### 4. 検証メソッドの追加
```csharp
public partial record VRChatInstance
{
    /// <summary>
    /// インスタンス情報の妥当性を検証
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (!IsValidWorldId(WorldId))
            errors.Add($"Invalid WorldId format: {WorldId}");

        if (!IsValidInstanceName(InstanceName))
            errors.Add($"Invalid InstanceName format: {InstanceName}");

        if (Owner != null && !IsValidOwnerId(Owner.Id))
            errors.Add($"Invalid OwnerId format: {Owner.Id}");

        if (Nonce != null && !IsValidNonce(Nonce))
            errors.Add($"Invalid Nonce format: {Nonce}");

        // 業務ルールの検証
        if (IsPrivate && Owner == null)
            errors.Add("Private instances must have an owner.");

        if (IsGroup && (Owner == null || Owner.Type != OwnerType.Group))
            errors.Add("Group instances must have a group owner.");

        return errors.Count == 0;
    }

    private static bool IsValidOwnerId(string ownerId)
    {
        return ownerId.StartsWith("usr_") || ownerId.StartsWith("grp_");
    }

    private static bool IsValidNonce(string nonce)
    {
        return Guid.TryParse(nonce, out _);
    }
}
```

#### 5. ユーティリティメソッドの追加
```csharp
public partial record VRChatInstance
{
    /// <summary>
    /// インスタンスの表示名を生成
    /// </summary>
    public string GetDisplayName()
    {
        var typeName = Type switch
        {
            InstanceType.Public => "Public",
            InstanceType.Friends => "Friends",
            InstanceType.FriendsPlus => "Friends+",
            InstanceType.Invite => "Invite",
            InstanceType.InvitePlus => "Invite+",
            InstanceType.Group => "Group",
            InstanceType.GroupPlus => "Group+",
            InstanceType.GroupPublic => "Group Public",
            _ => "Unknown"
        };

        return $"{InstanceName} ({typeName})";
    }

    /// <summary>
    /// 地域の表示名を取得
    /// </summary>
    public string GetRegionDisplayName()
    {
        return Region switch
        {
            InstanceRegion.USWest => "US West",
            InstanceRegion.USEast => "US East",
            InstanceRegion.Europe => "Europe",
            InstanceRegion.Japan => "Japan",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// インスタンスの簡潔な説明を生成
    /// </summary>
    public string GetSummary()
    {
        return $"{GetDisplayName()} in {GetRegionDisplayName()}";
    }
}
```

#### 6. ファクトリーメソッドの追加
```csharp
public static class VRChatInstanceFactory
{
    /// <summary>
    /// パブリックインスタンスを作成
    /// </summary>
    public static VRChatInstance CreatePublic(string worldId, string instanceName, InstanceRegion region)
    {
        return new VRChatInstance
        {
            WorldId = worldId,
            InstanceName = instanceName,
            Type = InstanceType.Public,
            Owner = null,
            Region = region,
            Nonce = null
        };
    }

    /// <summary>
    /// フレンドインスタンスを作成
    /// </summary>
    public static VRChatInstance CreateFriends(string worldId, string instanceName, 
        string ownerId, InstanceRegion region, bool isPlus = false)
    {
        return new VRChatInstance
        {
            WorldId = worldId,
            InstanceName = instanceName,
            Type = isPlus ? InstanceType.FriendsPlus : InstanceType.Friends,
            Owner = OwnerInfoFactory.Create(ownerId),
            Region = region,
            Nonce = null
        };
    }
}
```

#### 7. ToString()の改善
```csharp
public override string ToString()
{
    var ownerInfo = Owner != null ? $" (Owner: {Owner.Id})" : "";
    return $"{WorldId}:{InstanceName} [{Type}] @ {Region}{ownerInfo}";
}
```

## セキュリティ考慮事項
- **低リスク**: 基本的なデータモデルのため、直接的なセキュリティリスクは最小限
- **データ検証**: ID形式の検証でインジェクション攻撃の予防

## 設計品質

### 良い点
- **明確性**: インスタンス情報の核となるプロパティが適切に定義
- **型安全性**: enumの適切な使用
- **ドキュメント**: 詳細なXMLコメント

### 改善点
- **検証不足**: 入力値の検証が不十分
- **可変性**: イミュータブル設計への移行が推奨
- **拡張性**: 将来的な機能追加への対応

## 総合評価
**評価: B+**

基本的なデータモデルとしては適切ですが、入力検証とイミュータブル設計の採用により、より堅牢で保守しやすいコードに改善できます。

## 推奨アクション
1. **高**: 入力値検証の実装
2. **中**: レコード型への移行検討
3. **中**: 所有者情報の型安全性向上
4. **低**: ユーティリティメソッドの追加
5. **低**: ファクトリーメソッドの実装