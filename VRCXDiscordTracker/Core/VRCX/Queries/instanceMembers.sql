WITH user_events AS (
    SELECT
        user_id,
        display_name,
        location,
        MAX(CASE WHEN type = 'OnPlayerJoined' THEN created_at END) AS last_join_at,
        MAX(CASE WHEN type = 'OnPlayerLeft'   THEN created_at END) AS last_leave_at
    FROM
        gamelog_join_leave
    WHERE
        created_at BETWEEN :join_created_at
            AND COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
            AND location = :location
    GROUP BY
        user_id,
        display_name,
        location
)
SELECT
    ue.user_id,
    ue.display_name,
    ue.last_join_at,
    ue.last_leave_at,
    -- 現在インスタンスにいるか
    CASE
        WHEN ue.last_leave_at IS NULL         THEN TRUE
        WHEN ue.last_join_at  > ue.last_leave_at THEN TRUE
        ELSE                                     FALSE
    END AS is_currently,
    -- インスタンスオーナーかどうか
    CASE
        WHEN instr(ue.location, ue.user_id) > 0 THEN TRUE
        ELSE                                     FALSE
    END AS is_instance_owner,
    -- フレンドかどうか
    CASE
        WHEN f.user_id IS NOT NULL THEN TRUE
        ELSE                         FALSE
    END AS is_friend
FROM
    user_events ue
    -- フレンドテーブルと照合
    LEFT JOIN @{friendTableName} f
    ON ue.user_id = f.user_id
ORDER BY
    ue.user_id;