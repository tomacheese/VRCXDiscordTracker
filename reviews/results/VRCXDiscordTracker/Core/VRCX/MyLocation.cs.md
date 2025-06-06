# MyLocation.cs レビュー結果

## 概要
MyLocation.csは、ユーザーがVRChatで参加していた/いるインスタンスの詳細情報を格納するデータモデルクラスです。参加・退出のタイムスタンプ、推定値、関連するインスタンス情報を保持します。

## コード品質評価

### 良い点
1. **包括的なデータモデル**: VRCXのロケーション履歴に必要な全データを網羅
2. **XMLドキュメント**: 各プロパティの意味が明確に説明されている
3. **required修飾子**: null安全性の向上
4. **適切な関連**: VRChatInstanceとの適切な関連付け

### 懸念事項・改善提案

#### 1. **重要**: データ複製と整合性の問題
```csharp
// 問題: 同じワールド情報が重複
public required VRChatInstance LocationInstance { get; set; }  // ワールドIDを含む
public required string? WorldName { get; set; }               // 重複
public required string? WorldId { get; set; }                 // 重複

// 推奨: データの正規化
public class MyLocation
{
    // 核となる参照
    public required VRChatInstance LocationInstance { get; set; }
    
    // 追加のワールド情報（LocationInstanceにないもののみ）
    public required string? WorldName { get; set; }
    
    // 計算プロパティでWorldIdを取得
    public string? WorldId => LocationInstance?.WorldId;
}
```

#### 2. **重要**: DateTime処理の改善
```csharp
// 現在: DateTime使用（タイムゾーン不明）
public required DateTime JoinCreatedAt { get; set; }
public required DateTime? LeaveCreatedAt { get; set; }

// 推奨: DateTimeOffset使用
public required DateTimeOffset JoinCreatedAt { get; set; }
public required DateTimeOffset? LeaveCreatedAt { get; set; }
public required DateTimeOffset? NextJoinCreatedAt { get; set; }
public required DateTimeOffset? EstimatedLeaveCreatedAt { get; set; }

// または、UTC明示
/// <summary>
/// インスタンスに参加した日時（UTC）
/// </summary>
public required DateTime JoinCreatedAtUtc { get; set; }
```

#### 3. 複雑なデータ構造の整理
```csharp
// 推奨: ネストした構造で整理
public class MyLocation
{
    public required long JoinId { get; set; }
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }
    public required string LocationId { get; set; }
    public required VRChatInstance LocationInstance { get; set; }
    
    // タイムスタンプを構造化
    public required LocationTimestamps Timestamps { get; set; }
    
    // ワールド情報を構造化
    public required WorldInfo World { get; set; }
}

public record LocationTimestamps(
    DateTime JoinCreatedAt,
    long JoinTime,
    long? LeaveId,
    DateTime? LeaveCreatedAt,
    long? LeaveTime,
    DateTime? NextJoinCreatedAt,
    DateTime? EstimatedLeaveCreatedAt
);

public record WorldInfo(string? WorldName, string? WorldId);
```

#### 4. 業務ロジックメソッドの追加
```csharp
public class MyLocation
{
    // 既存のプロパティ...

    /// <summary>
    /// 現在もこのインスタンスに参加中かどうか
    /// </summary>
    public bool IsCurrentlyInInstance => 
        LeaveCreatedAt == null && NextJoinCreatedAt == null;

    /// <summary>
    /// インスタンス滞在時間を計算
    /// </summary>
    public TimeSpan? GetSessionDuration()
    {
        var endTime = LeaveCreatedAt ?? EstimatedLeaveCreatedAt ?? DateTime.UtcNow;
        return endTime - JoinCreatedAt;
    }

    /// <summary>
    /// セッション時間（ミリ秒）
    /// VRCXのLeaveTimeが利用可能な場合はそれを、そうでなければ計算値を返す
    /// </summary>
    public long GetSessionDurationMs()
    {
        if (LeaveTime.HasValue)
            return LeaveTime.Value;
            
        var duration = GetSessionDuration();
        return duration?.Milliseconds ?? 0;
    }

    /// <summary>
    /// 推定退出時刻の精度を評価
    /// </summary>
    public LocationAccuracy GetEstimatedLeaveAccuracy()
    {
        if (LeaveCreatedAt.HasValue)
            return LocationAccuracy.Exact;
            
        if (NextJoinCreatedAt.HasValue)
            return LocationAccuracy.EstimatedFromNextJoin;
            
        return LocationAccuracy.Unknown;
    }
}

public enum LocationAccuracy
{
    Exact,              // 正確な退出時刻
    EstimatedFromNextJoin, // 次回参加時刻から推定
    Unknown             // 不明
}
```

#### 5. データ検証の追加
```csharp
public class MyLocation
{
    // 既存のプロパティ...

    /// <summary>
    /// データの整合性を検証
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        // 必須フィールドの検証
        if (string.IsNullOrWhiteSpace(UserId))
            errors.Add("UserId cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(LocationId))
            errors.Add("LocationId cannot be null or empty.");

        // 時系列の検証
        if (LeaveCreatedAt.HasValue && LeaveCreatedAt < JoinCreatedAt)
            errors.Add("Leave time cannot be before join time.");

        if (NextJoinCreatedAt.HasValue && NextJoinCreatedAt < JoinCreatedAt)
            errors.Add("Next join time cannot be before current join time.");

        // データ整合性の検証
        if (LeaveId.HasValue && !LeaveCreatedAt.HasValue)
            errors.Add("LeaveId is set but LeaveCreatedAt is null.");

        if (LeaveTime.HasValue && !LeaveCreatedAt.HasValue)
            errors.Add("LeaveTime is set but LeaveCreatedAt is null.");

        // ワールド情報の整合性
        if (LocationInstance != null && WorldId != null && LocationInstance.WorldId != WorldId)
            errors.Add("WorldId does not match LocationInstance.WorldId.");

        return errors.Count == 0;
    }
}
```

#### 6. 比較とソート機能
```csharp
public class MyLocation : IComparable<MyLocation>, IEquatable<MyLocation>
{
    // 既存のプロパティ...

    public int CompareTo(MyLocation? other)
    {
        if (other == null) return 1;
        return JoinCreatedAt.CompareTo(other.JoinCreatedAt);
    }

    public bool Equals(MyLocation? other)
    {
        return other != null && JoinId == other.JoinId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MyLocation);
    }

    public override int GetHashCode()
    {
        return JoinId.GetHashCode();
    }
}
```

#### 7. ファクトリーメソッドの追加
```csharp
public static class MyLocationFactory
{
    /// <summary>
    /// 新規参加レコードを作成
    /// </summary>
    public static MyLocation CreateJoinRecord(
        long joinId,
        string userId,
        string displayName,
        string locationId,
        VRChatInstance instance,
        DateTime joinTime,
        string? worldName = null)
    {
        return new MyLocation
        {
            JoinId = joinId,
            UserId = userId,
            DisplayName = displayName,
            LocationId = locationId,
            LocationInstance = instance,
            JoinCreatedAt = joinTime,
            JoinTime = 0, // 通常は0
            LeaveId = null,
            LeaveCreatedAt = null,
            LeaveTime = null,
            NextJoinCreatedAt = null,
            EstimatedLeaveCreatedAt = null,
            WorldName = worldName,
            WorldId = instance.WorldId
        };
    }

    /// <summary>
    /// 退出情報を追加してレコードを更新
    /// </summary>
    public static MyLocation WithLeaveInfo(
        MyLocation location,
        long leaveId,
        DateTime leaveTime,
        long sessionDurationMs)
    {
        return location with
        {
            LeaveId = leaveId,
            LeaveCreatedAt = leaveTime,
            LeaveTime = sessionDurationMs
        };
    }
}
```

## セキュリティ考慮事項
- **個人情報**: UserIdとDisplayNameの適切な取り扱いが必要
- **低リスク**: 基本的なデータモデルのため、直接的なセキュリティリスクは最小限

## 設計品質

### 良い点
- **包括性**: VRCXの全ロケーション情報を網羅
- **明確性**: プロパティの意味が明確

### 改善点
- **データ重複**: WorldIdとWorldNameの重複
- **複雑性**: 多くのプロパティで理解が困難
- **検証不足**: データ整合性チェックの欠如

## パフォーマンス
- **メモリ使用量**: 多くのプロパティでメモリ使用量が多い
- **シリアライゼーション**: 複雑な構造でシリアライゼーションコストが高い

## 総合評価
**評価: B**

包括的なデータモデルですが、データの重複と複雑さが問題です。リファクタリングにより、よりクリーンで保守しやすい設計に改善できます。

## 推奨アクション
1. **高**: データ重複の除去（WorldId, WorldName）
2. **高**: DateTimeOffsetの使用またはUTC明示
3. **中**: データ構造の整理（ネスト構造の使用）
4. **中**: データ検証メソッドの追加
5. **低**: 業務ロジックメソッドの追加
6. **低**: IComparableの実装