# InstanceMember.cs レビュー結果

## 概要
InstanceMember.csは、VRCXのインスタンスメンバー情報を表すデータモデルクラスです。ユーザーの参加・退出情報、フレンド状態、オーナー権限などを保持します。

## コード品質評価

### 良い点
1. **明確なデータモデル**: プロパティの意味が明確
2. **XMLドキュメント**: 各プロパティが適切に文書化
3. **required修飾子**: null安全性の向上
4. **適切な命名**: プロパティ名が直感的

### 懸念事項・改善提案

#### 1. **重要**: DateTime処理の改善
```csharp
// 現在: DateTime?の使用
public required DateTime? LastJoinAt { get; set; }
public required DateTime? LastLeaveAt { get; set; }

// 推奨: DateTimeOffset使用でタイムゾーン対応
public required DateTimeOffset? LastJoinAt { get; set; }
public required DateTimeOffset? LastLeaveAt { get; set; }

// または、UTC明示
/// <summary>
/// 最終参加日時（UTC）
/// </summary>
public required DateTime? LastJoinAtUtc { get; set; }

/// <summary>
/// 最終退出日時（UTC）
/// </summary>
public required DateTime? LastLeaveAtUtc { get; set; }
```

#### 2. データ整合性の向上
```csharp
// 推奨: 業務ルールの検証
public class InstanceMember
{
    private DateTime? _lastJoinAt;
    private DateTime? _lastLeaveAt;

    public required DateTime? LastJoinAt 
    { 
        get => _lastJoinAt;
        set
        {
            if (value.HasValue && _lastLeaveAt.HasValue && value > _lastLeaveAt)
            {
                throw new ArgumentException("Join time cannot be after leave time.");
            }
            _lastJoinAt = value;
        }
    }

    public required DateTime? LastLeaveAt 
    { 
        get => _lastLeaveAt;
        set
        {
            if (value.HasValue && _lastJoinAt.HasValue && value < _lastJoinAt)
            {
                throw new ArgumentException("Leave time cannot be before join time.");
            }
            _lastLeaveAt = value;
        }
    }

    /// <summary>
    /// メンバーが現在インスタンスに参加中かどうかを計算
    /// </summary>
    public bool IsCurrentlyInInstance => 
        LastJoinAt.HasValue && (!LastLeaveAt.HasValue || LastJoinAt > LastLeaveAt);
}
```

#### 3. イミュータブル設計の検討
```csharp
// 推奨: レコード型での実装
public record InstanceMember
{
    /// <summary>
    /// インスタンスメンバーのID
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// インスタンスメンバーの名前
    /// </summary>
    public required string DisplayName { get; init; }

    // 時刻データをまとめたネストレコード
    public required MemberTimestamps Timestamps { get; init; }
    
    // 状態データをまとめたネストレコード
    public required MemberStatus Status { get; init; }
}

public record MemberTimestamps(DateTime? LastJoinAt, DateTime? LastLeaveAt);

public record MemberStatus(bool IsCurrently, bool IsInstanceOwner, bool IsFriend);
```

#### 4. 検証メソッドの追加
```csharp
public class InstanceMember
{
    // 既存のプロパティ...

    /// <summary>
    /// メンバーデータの整合性を検証
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(UserId))
            errors.Add("UserId cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(DisplayName))
            errors.Add("DisplayName cannot be null or empty.");

        if (LastJoinAt.HasValue && LastLeaveAt.HasValue && LastJoinAt > LastLeaveAt)
            errors.Add("LastJoinAt cannot be after LastLeaveAt.");

        // 現在参加中なのに退出日時がある場合の警告
        if (IsCurrently && LastLeaveAt.HasValue && LastJoinAt.HasValue && LastLeaveAt > LastJoinAt)
            errors.Add("Member marked as currently in instance but has recent leave time.");

        return errors.Count == 0;
    }

    /// <summary>
    /// インスタンス滞在時間を計算
    /// </summary>
    public TimeSpan? GetSessionDuration()
    {
        if (!LastJoinAt.HasValue) return null;
        
        var endTime = LastLeaveAt ?? DateTime.UtcNow;
        return endTime - LastJoinAt.Value;
    }
}
```

#### 5. 比較機能の追加
```csharp
public class InstanceMember : IEquatable<InstanceMember>
{
    // 既存のプロパティ...

    public bool Equals(InstanceMember? other)
    {
        return other != null && UserId == other.UserId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as InstanceMember);
    }

    public override int GetHashCode()
    {
        return UserId.GetHashCode();
    }

    public static bool operator ==(InstanceMember? left, InstanceMember? right)
    {
        return EqualityComparer<InstanceMember>.Default.Equals(left, right);
    }

    public static bool operator !=(InstanceMember? left, InstanceMember? right)
    {
        return !(left == right);
    }
}
```

#### 6. ファクトリーメソッドの追加
```csharp
public static class InstanceMemberFactory
{
    /// <summary>
    /// 新規参加メンバーを作成
    /// </summary>
    public static InstanceMember CreateJoiningMember(
        string userId, 
        string displayName, 
        DateTime joinTime,
        bool isInstanceOwner = false,
        bool isFriend = false)
    {
        return new InstanceMember
        {
            UserId = userId,
            DisplayName = displayName,
            LastJoinAt = joinTime,
            LastLeaveAt = null,
            IsCurrently = true,
            IsInstanceOwner = isInstanceOwner,
            IsFriend = isFriend
        };
    }

    /// <summary>
    /// 退出したメンバーとして更新
    /// </summary>
    public static InstanceMember MarkAsLeft(InstanceMember member, DateTime leaveTime)
    {
        return member with
        {
            LastLeaveAt = leaveTime,
            IsCurrently = false
        };
    }
}
```

#### 7. ToString()の実装
```csharp
public override string ToString()
{
    var status = IsCurrently ? "In Instance" : "Left";
    var ownerFlag = IsInstanceOwner ? " (Owner)" : "";
    var friendFlag = IsFriend ? " (Friend)" : "";
    
    return $"{DisplayName} [{UserId}] - {status}{ownerFlag}{friendFlag}";
}
```

## セキュリティ考慮事項
- **低リスク**: 基本的なデータモデルのため、直接的なセキュリティリスクは最小限
- **データ検証**: ユーザーIDの形式検証が推奨される

## 設計品質

### 良い点
- **シンプルさ**: 明確なデータ構造
- **型安全性**: required修飾子の適切な使用

### 改善点
- **不変性**: 可能であればイミュータブル設計が推奨
- **検証機能**: データ整合性チェックの不足

## パフォーマンス
- **メモリ効率**: 軽量なデータクラス
- **コピーコスト**: 参照型プロパティのみ

## 総合評価
**評価: B+**

シンプルで理解しやすいデータモデルですが、データ整合性チェックとタイムゾーン対応の改善が推奨されます。基本的な機能としては適切に設計されています。

## 推奨アクション
1. **高**: DateTimeOffset使用またはUTC明示
2. **中**: データ整合性検証の追加
3. **中**: IEquatableの実装
4. **低**: ファクトリーメソッドの追加
5. **低**: ToString()の実装