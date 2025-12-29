-- 指定されたインスタンスのメンバー情報を取得するクエリ
-- 特定の時間範囲内でインスタンスに参加したユーザーの情報を取得し、
-- 現在の参加状態、インスタンスオーナーかどうか、フレンドかどうかを判定する

WITH user_events AS (
    -- 指定された時間範囲内で各ユーザーの最新の参加/退出イベントを集計
    SELECT
        user_id,                         -- ユーザーID
        display_name,                    -- 表示名
        location,                        -- ロケーションID
        -- 最後に参加した日時 (OnPlayerJoinedイベント)
        MAX(CASE WHEN type = 'OnPlayerJoined' THEN created_at END) AS last_join_at,
        -- 最後に退出した日時 (OnPlayerLeftイベント)
        MAX(CASE WHEN type = 'OnPlayerLeft'   THEN created_at END) AS last_leave_at
    FROM
        gamelog_join_leave
    WHERE
        -- 指定された時間範囲内のイベントのみ
        created_at BETWEEN :join_created_at
            AND COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
            -- 指定されたロケーションのみ
            AND location = :location
    GROUP BY
        user_id,
        display_name,
        location
)
SELECT
    ue.user_id,                          -- ユーザーID
    ue.display_name,                     -- 表示名
    ue.last_join_at,                     -- 最終参加日時
    ue.last_leave_at,                    -- 最終退出日時
    -- 現在インスタンスにいるかどうかを判定
    -- 退出イベントがない、または最後の参加が最後の退出より後なら現在参加中
    CASE
        WHEN ue.last_leave_at IS NULL         THEN TRUE
        WHEN ue.last_join_at  > ue.last_leave_at THEN TRUE
        ELSE                                     FALSE
    END AS is_currently,
    -- インスタンスオーナーかどうかを判定
    -- ロケーションIDにユーザーIDが含まれていればオーナー
    CASE
        WHEN instr(ue.location, ue.user_id) > 0 THEN TRUE
        ELSE                                     FALSE
    END AS is_instance_owner,
    -- フレンドかどうかを判定
    -- フレンドテーブルに存在すればフレンド
    CASE
        WHEN f.user_id IS NOT NULL THEN TRUE
        ELSE                         FALSE
    END AS is_friend
FROM
    user_events ue
    -- フレンドテーブルと結合してフレンド情報を取得
    -- @{friendTableName} は実行時に動的に置換される
    LEFT JOIN @{friendTableName} f
    ON ue.user_id = f.user_id
ORDER BY
    last_join_at;                        -- 参加日時の昇順で並べ替え