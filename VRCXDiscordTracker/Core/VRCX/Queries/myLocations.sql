WITH
joined AS (
    SELECT
        id               AS join_id,
        user_id,
        display_name,
        location,
        created_at       AS join_created_at,
        time             AS join_time
    FROM gamelog_join_leave
    WHERE
        user_id = :target_user_id
        AND type = 'OnPlayerJoined'
),

next_leave AS (
    SELECT
        j.join_id,
        MIN(l.created_at) AS leave_created_at
    FROM joined j
    LEFT JOIN gamelog_join_leave l
    ON l.user_id     = j.user_id
        AND l.type   = 'OnPlayerLeft'
        AND l.location = j.location
        AND l.created_at > j.join_created_at
    GROUP BY
        j.join_id
),

paired AS (
    SELECT
        j.*,
        nl.leave_created_at,
        l.id   AS leave_id,
        l.time AS leave_time
    FROM joined j
    LEFT JOIN next_leave nl
    ON j.join_id = nl.join_id
    LEFT JOIN gamelog_join_leave l
    ON l.user_id    = j.user_id
        AND l.type  = 'OnPlayerLeft'
        AND l.location = j.location
        AND l.created_at = nl.leave_created_at
),

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
        LEAD(p.join_created_at) OVER (
            PARTITION BY p.user_id
            ORDER BY p.join_created_at
        ) AS next_join_created_at,
        COALESCE(
            p.leave_created_at,
            LEAD(p.join_created_at) OVER (
                PARTITION BY p.user_id
                ORDER BY p.join_created_at
            )
        ) AS estimated_leave_created_at
    FROM paired p
)

SELECT
    DISTINCT f.*,
    gl.world_name,
    gl.world_id
FROM final f
LEFT JOIN gamelog_location gl
ON f.location = gl.location
WHERE
    f.estimated_leave_created_at IS NULL
    OR f.estimated_leave_created_at >= datetime('now','-12 hours')
ORDER BY
    f.join_id;