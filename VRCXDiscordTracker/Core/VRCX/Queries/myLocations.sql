-- 指定ユーザーのロケーション履歴を取得するクエリ
-- 参加イベントと退出イベントをペアリングし、ワールド情報も含める

WITH
  -- 1. 参加イベントを抽出
  joined AS (
    SELECT
      id               AS join_id,       -- 参加イベントのID
      user_id,                           -- ユーザーID
      display_name,                      -- 表示名
      location,                          -- ロケーションID
      created_at       AS join_created_at,  -- 参加日時
      time             AS join_time      -- 参加時刻 (基本的に0)
    FROM gamelog_join_leave
    WHERE
      user_id = :target_user_id          -- 対象ユーザーでフィルタ
      AND type = 'OnPlayerJoined'        -- 参加イベントのみ
  ),

  -- 2. 各参加イベントに対応する次の退出イベントを見つける
  next_leave AS (
    SELECT
      j.join_id,
      MIN(l.created_at) AS leave_created_at  -- 同一ロケーションでの次の退出日時
    FROM joined j
    LEFT JOIN gamelog_join_leave l
      ON l.user_id = j.user_id           -- 同一ユーザー
     AND l.type = 'OnPlayerLeft'         -- 退出イベント
     AND l.location = j.location         -- 同一ロケーション
     AND l.created_at > j.join_created_at  -- 参加後の退出
    GROUP BY j.join_id
  ),

  -- 3. 参加イベントと退出イベントをペアリング
  paired AS (
    SELECT
      j.*,
      nl.leave_created_at,               -- 退出日時
      l.id AS leave_id,                  -- 退出イベントのID
      l.time AS leave_time               -- 滞在時間 (ミリ秒)
    FROM joined j
    LEFT JOIN next_leave nl
      ON j.join_id = nl.join_id
    LEFT JOIN gamelog_join_leave l
      ON l.user_id = j.user_id
     AND l.type = 'OnPlayerLeft'
     AND l.location = j.location
     AND l.created_at = nl.leave_created_at
  ),

  -- 4. 次の参加日時と推定退出日時を計算
  final AS (
    SELECT
      p.join_id,
      p.user_id,
      p.display_name,
      p.location,
      p.join_created_at,
      p.join_time,
      p.leave_id,
      p.leave_created_at,
      p.leave_time,
      -- 次の参加日時 (別のロケーションへの移動)
        LEAD(p.join_created_at) OVER (
            PARTITION BY p.user_id
            ORDER BY p.join_created_at
        ) AS next_join_created_at,
      -- 推定退出日時: 実際の退出日時がなければ次の参加日時を使用
      COALESCE(
        p.leave_created_at,
        LEAD(p.join_created_at) OVER (
          PARTITION BY p.user_id
          ORDER BY p.join_created_at
        )
      ) AS estimated_leave_created_at
    FROM paired p
  ),

  -- 5. 各ロケーションの最新のワールド情報を取得
  locations AS (
    SELECT
        location,
        world_name,                      -- ワールド名
        world_id,                        -- ワールドID
        group_name                       -- グループ名
    FROM (
        SELECT
            gl.*,
            -- 同一ロケーションで最新のレコードを特定
            ROW_NUMBER() OVER (PARTITION BY location ORDER BY rowid DESC) AS rn
        FROM gamelog_location gl
    ) t
    WHERE rn = 1                         -- 最新のレコードのみ
  ),

  -- 6. 最終結果を制限数まで取得 (新しい順)
  limited AS (
    SELECT
      f.*,
      l.world_name,
      l.world_id,
      l.group_name
    FROM final f
    LEFT JOIN locations l
      ON f.location = l.location
    ORDER BY f.join_created_at DESC      -- 新しい順
    LIMIT :location_count                -- 取得件数を制限
  )

-- 最終的な結果を参加ID順に並べ替えて返す
SELECT
  *
FROM limited
ORDER BY
  join_id;                               -- 参加ID順 (古い順)