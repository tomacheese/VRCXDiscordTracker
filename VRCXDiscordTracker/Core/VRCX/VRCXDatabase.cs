using System.Data.SQLite;
using System.Reflection;

namespace VRCXDiscordTracker.Core.VRCX;
internal class VRCXDatabase
{
    private readonly SQLiteConnection _conn;

    public VRCXDatabase(string databasePath)
    {
        var sqlConnStr = new SQLiteConnectionStringBuilder
        {
            DataSource = databasePath,
            ReadOnly = true,
        };

        _conn = new SQLiteConnection(sqlConnStr.ToString());
        _conn.Open();
    }

    public void Open()
    {
        if (_conn.State == System.Data.ConnectionState.Open)
        {
            return;
        }
        _conn.Open();
    }
    public void Close()
    {
        _conn.Close();
    }

    public void Dispose()
    {
        _conn.Close();
        _conn.Dispose();
    }

    public string GetVRChatUserId()
    {
        using var cmd = new SQLiteCommand(_conn);
        // configs テーブルで、key = "config:lastuserloggedin" の value
        cmd.CommandText = "SELECT value FROM configs WHERE key = 'config:lastuserloggedin'";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetString(0);
        }

        throw new Exception("VRChat User ID not found in database.");
    }

    public List<MyLocation> GetMyLocations(string vrchatUserId)
    {
        Console.WriteLine($"GetMyLocations: {vrchatUserId}");
        string sql = GetEmbedFileContent("VRCXDiscordTracker.Core.VRCX.Queries.myLocations.sql");

        var myLocations = new List<MyLocation>();
        using (var cmd = new SQLiteCommand(_conn))
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue(":target_user_id", vrchatUserId);
            using var reader = cmd.ExecuteReader();
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
                        LeaveId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                        LeaveCreatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                        LeaveTime = reader.IsDBNull(8) ? null : reader.GetInt64(8),
                        NextJoinCreatedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9)),
                        EstimatedLeaveCreatedAt = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10)),
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

        // Location が local: で始まる場合は、ローカルインスタンスなので除外
        return myLocations.FindAll(
            myLocation => !myLocation.Location.StartsWith("local:")
        );
    }

    public List<InstanceMember> GetInstanceMembers(string vrchatUserId, string location, DateTime joinCreatedAt, DateTime? estimatedLeaveAt)
    {
        Console.WriteLine($"GetInstanceMembers: {vrchatUserId}, {joinCreatedAt.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffffffZ}, {estimatedLeaveAt?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}");
        var sanitizedUserId = vrchatUserId.Replace("_", "").Replace("-", "");
        var friendTableName = sanitizedUserId + "_friend_log_current";
        var sql = GetEmbedFileContent("VRCXDiscordTracker.Core.VRCX.Queries.instanceMembers.sql").Replace("@{friendTableName}", friendTableName);

        var instanceMembers = new List<InstanceMember>();
        using (var cmd = new SQLiteCommand(_conn))
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue(":join_created_at", joinCreatedAt.AddSeconds(-1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
            cmd.Parameters.AddWithValue(":estimated_leave_created_at", estimatedLeaveAt?.AddSeconds(1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
            cmd.Parameters.AddWithValue(":location", location);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // 取得したデータを処理
                var instanceMember = new InstanceMember
                {
                    UserId = reader.GetString(0),
                    DisplayName = reader.GetString(1),
                    LastJoinAt = DateTime.Parse(reader.GetString(2)),
                    LastLeaveAt = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
                    IsCurrently = reader.GetBoolean(4),
                    IsInstanceOwner = reader.GetBoolean(5),
                    IsFriend = reader.GetBoolean(6),
                };
                instanceMembers.Add(instanceMember);
            }
        }

        return instanceMembers;
    }

    private static string GetEmbedFileContent(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(name) ?? throw new Exception(
                $"Resource '{name}' not found in assembly '{assembly.FullName}'."
            );
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
