# instanceMembers.sql レビュー

## 概要

`instanceMembers.sql`はVRCXのデータベースから指定されたインスタンスにいるメンバー情報を取得するためのSQLクエリです。インスタンスの参加・退出履歴からユーザーの状態を特定し、フレンドテーブルと結合して、フレンド情報も取得しています。

## 良い点

1. 共通テーブル式（CTE）を使用して、クエリの読みやすさと保守性を向上させている
2. パラメータ化されたクエリで、SQLインジェクションを防止している
3. 複雑な条件に対してCASE文を効果的に使用している
4. 結果を参加時間でソートして、理解しやすい順序で返している

## 改善点

### 1. SQLインジェクションのリスク

```sql
LEFT JOIN @{friendTableName} f
```

この部分では、テーブル名がプレースホルダ`@{friendTableName}`として文字列置換されています。これはC#コード側で文字列置換されているため、SQLインジェクションのリスクがあります。通常、パラメータ化クエリでテーブル名を動的に変更することはできませんが、安全なアプローチとしては、ホワイトリストベースのテーブル名検証を実装することが望ましいです。

```sql
-- 代替案: 事前に検証されたテーブル名をSQLに直接埋め込むのではなく、
-- 以下のようなパターンでクエリを構築する
WITH user_events AS (
    -- 既存のコード
)
SELECT
    ue.user_id,
    ue.display_name,
    -- 他のカラム...
FROM
    user_events ue
    -- フレンドテーブルと照合（テーブル名は事前に検証されるべき）
    LEFT JOIN @{friendTableName} f
    ON ue.user_id = f.user_id
```

C#側での改善案：

```csharp
// 正規表現で厳格にフォーマットを検証
if (!Regex.IsMatch(sanitizedUserId + "_friend_log_current", "^[a-zA-Z0-9_]+$"))
{
    throw new ArgumentException("Invalid table name format");
}
```

### 2. ロケーション検索の最適化

現在のクエリでは、`location`列を使用してフィルタリングしていますが、この列にインデックスが存在しない場合、パフォーマンスが低下する可能性があります。

```sql
WHERE
    created_at BETWEEN :join_created_at
        AND COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
        AND location = :location
```

VRCXデータベースの構造に基づいて、適切なインデックスが存在するかを確認し、必要に応じてインデックスを作成することを検討すべきです。特に、`created_at`と`location`の複合インデックスが有効かもしれません。

```sql
-- 仮想的なインデックス作成例（実際には管理者権限が必要）
CREATE INDEX idx_gamelog_join_leave_location_created_at
ON gamelog_join_leave (location, created_at);
```

### 3. NULL値の処理の明確化

現在のクエリでは、`last_join_at`や`last_leave_at`がNULLの場合の処理が若干不明確です。特に、`is_currently`の計算ロジックでは、より明示的なNULL処理が望ましいかもしれません。

```sql
-- 現在インスタンスにいるか（より明示的なNULL処理）
CASE
    WHEN ue.last_join_at IS NULL THEN FALSE -- 参加記録がない場合は明らかに現在いない
    WHEN ue.last_leave_at IS NULL THEN TRUE -- 退出記録がない場合は現在もいる
    WHEN ue.last_join_at > ue.last_leave_at THEN TRUE -- 最後の参加が最後の退出より後なら現在もいる
    ELSE FALSE
END AS is_currently,
```

### 4. インスタンスオーナー判定の改善

インスタンスオーナーの判定には`instr`関数を使用していますが、これは部分文字列マッチングであるため、潜在的に誤検出する可能性があります。

```sql
-- インスタンスオーナーかどうか
CASE
    WHEN instr(ue.location, ue.user_id) > 0 THEN TRUE
    ELSE FALSE
END AS is_instance_owner,
```

より正確な判定ロジックを実装するには、ロケーションIDの構造を理解し、正規表現や特定のパターンマッチングを使用することが望ましいです。

```sql
-- より具体的なパターンマッチング（例）
CASE
    -- PrivateインスタンスでユーザーIDが所有者として表示されている場合
    WHEN ue.location LIKE '%~private(' || ue.user_id || ')%' THEN TRUE
    -- FriendsインスタンスでユーザーIDが所有者として表示されている場合
    WHEN ue.location LIKE '%~friends(' || ue.user_id || ')%' THEN TRUE
    -- 他の所有者パターン...
    ELSE FALSE
END AS is_instance_owner,
```

ただし、SQLiteでは複雑な正規表現サポートが限られているため、実際にはC#側で処理する方が適切かもしれません。

### 5. パフォーマンス最適化

大規模なログデータベースの場合、クエリのパフォーマンスが懸念されます。特に`GROUP BY`操作と`MAX`関数の組み合わせは処理に時間がかかる可能性があります。

```sql
-- より効率的なアプローチとして、サブクエリやウィンドウ関数を検討
WITH join_events AS (
    SELECT
        user_id,
        display_name,
        location,
        created_at AS join_at,
        ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY created_at DESC) AS rn
    FROM
        gamelog_join_leave
    WHERE
        created_at BETWEEN :join_created_at AND COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
        AND location = :location
        AND type = 'OnPlayerJoined'
),
leave_events AS (
    SELECT
        user_id,
        created_at AS leave_at,
        ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY created_at DESC) AS rn
    FROM
        gamelog_join_leave
    WHERE
        created_at BETWEEN :join_created_at AND COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
        AND location = :location
        AND type = 'OnPlayerLeft'
)
SELECT
    je.user_id,
    je.display_name,
    je.join_at AS last_join_at,
    le.leave_at AS last_leave_at,
    -- 残りのロジック...
FROM
    join_events je
    LEFT JOIN leave_events le ON je.user_id = le.user_id AND le.rn = 1
WHERE
    je.rn = 1
```

ただし、SQLiteのバージョンによっては、ウィンドウ関数がサポートされていない場合もあります。

## セキュリティ上の懸念点

1. **SQLインジェクションリスク**: 前述のとおり、テーブル名の動的置換はリスクがあります。C#側での適切なサニタイズとバリデーションが必要です。

2. **データアクセス制御**: このクエリは他のユーザーの情報も取得するため、アプリケーションレベルでの適切なアクセス制御が必要です。

## 総合評価

全体として、このSQLクエリは目的を達成するために適切に設計されています。共通テーブル式を使用してクエリの可読性を高め、パラメータ化によりSQLインジェクションから保護しています。ただし、テーブル名の動的置換部分はセキュリティリスクとなるため、C#側での適切な検証が必要です。また、データベースのサイズが大きい場合は、インデックスの最適化やクエリの効率化を検討すべきです。
