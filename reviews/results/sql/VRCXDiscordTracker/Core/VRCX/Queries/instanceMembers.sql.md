# コードレビュー: VRCXDiscordTracker/Core/VRCX/Queries/instanceMembers.sql

## 概要

このSQLクエリはVRCXデータベースから、特定のインスタンスに存在した/存在するプレイヤー（メンバー）の情報を取得するために使用されています。CTEを使用してユーザーイベント（参加・退出）を集約し、現在インスタンスに居るかどうか、インスタンスのオーナーかどうか、フレンドかどうかの情報を含めた結果を返します。

## 良い点

- CTEを使用して複雑なクエリを構造化しており、理解しやすくなっています。
- CASE式を使用して、参加・退出イベントから各ユーザーの状態（現在インスタンスにいるかなど）を判断しています。
- パラメータ化されたクエリを使用し、SQLインジェクションを防止しています。
- GROUP BY句を使用して、各ユーザーの最新の参加・退出イベントをまとめています。
- フレンドテーブルとの結合により、フレンドかどうかの情報を追加しています。

## 改善点

### 1. テーブル名のプレースホルダー

```sql
-- テーブル名のプレースホルダーを使用しています
LEFT JOIN @{friendTableName} f
ON ue.user_id = f.user_id

-- これはSQLインジェクションのリスクがあります
-- 代わりに以下のようなアプローチを検討してください
-- 1. テーブル名のバリデーションを行うC#コード
-- 2. データベースビューの使用
-- 3. 動的SQLの代わりにストアドプロシージャの使用
```

### 2. CASE式の簡略化

```sql
-- 現在の条件式
CASE
    WHEN ue.last_leave_at IS NULL         THEN TRUE
    WHEN ue.last_join_at  > ue.last_leave_at THEN TRUE
    ELSE                                     FALSE
END AS is_currently

-- より簡潔に書くことができます
CASE
    WHEN ue.last_leave_at IS NULL OR ue.last_join_at > ue.last_leave_at THEN TRUE
    ELSE FALSE
END AS is_currently

-- または論理演算子を使用
(ue.last_leave_at IS NULL OR ue.last_join_at > ue.last_leave_at) AS is_currently
```

### 3. インスタンスオーナー判定のロジック

```sql
-- 現在のインスタンスオーナー判定
CASE
    WHEN instr(ue.location, ue.user_id) > 0 THEN TRUE
    ELSE                                     FALSE
END AS is_instance_owner

-- このロジックはVRChatのインスタンスID形式に依存しています
-- より堅牢なアプローチとしてはパターンマッチを検討してください
CASE
    WHEN location REGEXP ('^wrld_.+:' || user_id || '~.+$') THEN TRUE
    ELSE FALSE
END AS is_instance_owner
```

### 4. パフォーマンスの最適化

```sql
-- 現在のWHERE句
WHERE
    created_at BETWEEN :join_created_at
        AND COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
        AND location = :location

-- より明確にするために条件を分けることを検討してください
WHERE
    created_at >= :join_created_at
    AND created_at <= COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    AND location = :location
```

### 5. インデックスの考慮

```sql
-- 以下のカラムについてインデックスが存在しない場合、パフォーマンスが低下する可能性があります
WHERE
    created_at BETWEEN ... AND ...
    AND location = :location

-- 以下のようなインデックスを検討してください
-- CREATE INDEX idx_gamelog_join_leave_location_created_at ON gamelog_join_leave(location, created_at, type);
```

## セキュリティの問題

- テーブル名のプレースホルダー`@{friendTableName}`は、SQLインジェクションの脆弱性を持つ可能性があります。この値が適切にサニタイズされていない場合、悪意のあるSQLコードが実行される可能性があります。

## パフォーマンスの問題

- インスタンスに大量のプレイヤーが出入りする場合、`gamelog_join_leave`テーブルのレコード数が多くなり、効率が低下する可能性があります。適切なインデックスの作成を検討すべきです。
- `instr()`関数はフルテキスト検索に比べて効率が劣る場合があります。大規模なデータベースでパフォーマンスの問題が発生した場合には、より効率的な方法を検討してください。

## その他のコメント

- このクエリはVRCXのデータベース構造に強く依存しており、VRCXのスキーマが変更された場合には、このクエリも更新する必要があります。
- SQLクエリ内の行コメント（`--`で始まる行）は理解を助けていますが、各セクションの目的や重要な条件に関するより詳細なコメントを追加することで、メンテナンスが容易になります。
