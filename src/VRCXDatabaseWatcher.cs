using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VRCXDiscordTracker
{
    internal class VRCXDatabaseWatcher
    {
        private readonly string databasePath;
        /// <summary>
        /// キャンセル用のトークンソース
        /// </summary>
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public VRCXDatabaseWatcher(string databasePath)
        {
            // データベースパスが指定されていない場合は、デフォルトのVRCXデータベースパスを使用する
            string defaultLogPath = Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VRCX",
                "VRCX.sqlite3"
            ));
            this.databasePath = databasePath ?? defaultLogPath;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                await MonitorLoop(cts.Token);
            });
        }

        public void Stop()
        {
            cts.Cancel();
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Run();
                await Task.Delay(10000);
            }
        }

        public async Task Run()
        {
            var sqlConnStr = new SQLiteConnectionStringBuilder
            {
                DataSource = databasePath,
                ReadOnly = true,
            };

            using (var conn = new SQLiteConnection(sqlConnStr.ToString()))
            {
                conn.Open();

                var userId = GetVRChatUserId(conn);
                Console.WriteLine($"GetVRChatUserId: {userId}");
                var myLocations = GetMyLocations(conn, userId);
                Console.WriteLine($"GetMyLocations: {myLocations.Count}");
                foreach (var myLocation in myLocations)
                {
                    var instanceMembers = GetInstanceMembers(conn, userId, myLocation.JoinCreatedAt, myLocation.EstimatedLeaveCreatedAt);
                    Console.WriteLine($"GetInstanceMembers: {instanceMembers.Count}");

                    await new DiscordMessageManager(myLocation, instanceMembers).SendUpdateMessageAsync();
                }

                conn.Close();
            }
        }

        public string GetDatabasePath()
        {
            return databasePath;
        }

        private string GetVRChatUserId(SQLiteConnection conn)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                // configs テーブルで、key = "config:lastuserloggedin" の value
                cmd.CommandText = "SELECT value FROM configs WHERE key = 'config:lastuserloggedin'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0);
                    }
                }
            }

            return null;
        }

        private List<MyLocation> GetMyLocations(SQLiteConnection conn, string vrchatUserId)
        {
            Console.WriteLine($"GetMyLocations: {vrchatUserId}");
            const string sql = @"
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
            ";

            var myLocations = new List<MyLocation>();
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue(":target_user_id", vrchatUserId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            // 取得したデータを処理
                            var myLocation = new MyLocation
                            {
                                JoinId = reader.GetInt64(0),
                                UserId = reader.GetString(1),
                                DisplayName = reader.GetString(2),
                                Location = reader.GetString(3),
                                JoinCreatedAt = DateTime.Parse(reader.GetString(4)),
                                JoinTime = reader.GetInt64(5),
                                LeaveId = reader.IsDBNull(6) ? (long?)null : reader.GetInt64(6),
                                LeaveCreatedAt = reader.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(reader.GetString(7)),
                                LeaveTime = reader.IsDBNull(8) ? null : (long?)reader.GetInt64(8),
                                NextJoinCreatedAt = reader.IsDBNull(9) ? (DateTime?)null : DateTime.Parse(reader.GetString(9)),
                                EstimatedLeaveCreatedAt = reader.IsDBNull(10) ? (DateTime?)null : DateTime.Parse(reader.GetString(10)),
                                WorldName = reader.IsDBNull(11) ? null : reader.GetString(11),
                                WorldId = reader.IsDBNull(12) ? null : reader.GetString(12),
                            };
                            myLocations.Add(myLocation);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                }
            }

            return myLocations;
        }

        private List<InstanceMember> GetInstanceMembers(SQLiteConnection conn, string vrchatUserId, DateTime joinCreatedAt, DateTime? estimatedLeaveAt)
        {
            Console.WriteLine($"GetInstanceMembers: {vrchatUserId}, {joinCreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}, {estimatedLeaveAt?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}");
            var sanitizedUserId = vrchatUserId.Replace("_", "").Replace("-", "");
            var friendTableName = sanitizedUserId + "_friend_log_current";
            var sql = @"
            WITH user_events AS (
                SELECT
                    user_id,
                    display_name,
                    location,  -- 追加：location 列を取得
                    MAX(CASE WHEN type = 'OnPlayerJoined' THEN created_at END) AS last_join_at,
                    MAX(CASE WHEN type = 'OnPlayerLeft'   THEN created_at END) AS last_leave_at
                FROM
                    gamelog_join_leave
                WHERE
                    created_at BETWEEN :join_created_at
                        AND COALESCE(:estimated_leave_created_at, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
                GROUP BY
                    user_id,
                    display_name,
                    location   -- location を GROUP BY に追加
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
            ".Replace("@{friendTableName}", friendTableName);

            var instanceMembers = new List<InstanceMember>();
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue(":join_created_at", joinCreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
                cmd.Parameters.AddWithValue(":estimated_leave_created_at", estimatedLeaveAt?.AddMilliseconds(1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // 取得したデータを処理
                        var instanceMember = new InstanceMember
                        {
                            UserId = reader.GetString(0),
                            DisplayName = reader.GetString(1),
                            LastJoinAt = DateTime.Parse(reader.GetString(2)),
                            LastLeaveAt = reader.IsDBNull(3) ? (DateTime?)null : DateTime.Parse(reader.GetString(3)),
                            IsCurrently = reader.GetBoolean(4),
                            IsInstanceOwner = reader.GetBoolean(5),
                            IsFriend = reader.GetBoolean(6),
                        };
                        instanceMembers.Add(instanceMember);
                    }
                }
            }

            return instanceMembers;
        }
    }
}
