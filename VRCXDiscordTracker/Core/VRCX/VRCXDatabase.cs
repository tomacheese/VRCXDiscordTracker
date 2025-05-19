using System.Data.SQLite;
using System.Globalization;
using System.Reflection;
using VRCXDiscordTracker.Core.VRChat;

namespace VRCXDiscordTracker.Core.VRCX;

/// <summary>
/// VRCXのSQLiteデータベースを操作するクラス
/// </summary>
internal class VRCXDatabase
{
    /// <summary>
    /// SQLiteデータベースの接続
    /// </summary>
    private readonly SQLiteConnection _conn;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="databasePath">VRCXのSQLiteデータベースのパス</param>
    public VRCXDatabase(string databasePath)
    {
        var sqlConnStr = new SQLiteConnectionStringBuilder
        {
            DataSource = databasePath,
            ReadOnly = true,
        };

        _conn = new SQLiteConnection(sqlConnStr.ToString());
        Open();
    }

    /// <summary>
    /// データベース接続を開く
    /// </summary>
    public void Open()
    {
        Console.WriteLine("VRCXDatabase.Open()");
        if (_conn.State == System.Data.ConnectionState.Open)
        {
            return;
        }
        _conn.Open();
    }

    /// <summary>
    /// データベース接続を閉じる
    /// </summary>
    public void Close()
    {
        Console.WriteLine("VRCXDatabase.Close()");
        _conn.Close();
    }

    /// <summary>
    /// データベース接続を破棄する
    /// </summary>
    public void Dispose()
    {
        Console.WriteLine("VRCXDatabase.Dispose()");
        _conn.Close();
        _conn.Dispose();
    }

    /// <summary>
    /// VRChatのユーザーIDを取得する
    /// </summary>
    /// <returns>VRChatのユーザーID</returns>
    /// <exception cref="Exception">データベースにユーザーIDが存在しない場合</exception>
    public string GetVRChatUserId()
    {
        Console.WriteLine("VRCXDatabase.GetVRChatUserId()");
        using var cmd = new SQLiteCommand(_conn);
        // configs テーブルで、key = "config:lastuserloggedin" の value
        cmd.CommandText = "SELECT value FROM configs WHERE key = 'config:lastuserloggedin'";
        using SQLiteDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetString(0);
        }

        throw new Exception("VRChat User ID not found in database.");
    }

    /// <summary>
    /// 居た/居るインスタンス群の情報を取得する
    /// </summary>
    /// <param name="vrchatUserId">取得する対象ユーザーのVRChatユーザーID</param>
    /// <param name="locationCount">取得するインスタンスの数</param>
    /// <returns>居た/居るインスタンス群の情報</returns>
    public List<MyLocation> GetMyLocations(string vrchatUserId, int locationCount)
    {
        Console.WriteLine($"VRCXDatabase.GetMyLocations(): {vrchatUserId}");
        var sql = GetEmbedFileContent("VRCXDiscordTracker.Core.VRCX.Queries.myLocations.sql");

        var myLocations = new List<MyLocation>();
        using (var cmd = new SQLiteCommand(_conn))
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue(":target_user_id", vrchatUserId);
            cmd.Parameters.AddWithValue(":location_count", locationCount);
            using SQLiteDataReader reader = cmd.ExecuteReader();
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
                        LocationId = reader.GetString(3),
                        LocationInstance = LocationParser.Parse(reader.GetString(3)),
                        JoinCreatedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                        JoinTime = reader.GetInt64(5),
                        LeaveId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                        LeaveCreatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                        LeaveTime = reader.IsDBNull(8) ? null : reader.GetInt64(8),
                        NextJoinCreatedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
                        EstimatedLeaveCreatedAt = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
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
            myLocation => !myLocation.LocationId.StartsWith("local:")
        );
    }

    /// <summary>
    /// 指定したインスタンスのメンバー情報を取得する。同一インスタンスに複数回参加した場合を考慮して、自分の参加/退出日時を指定する。
    /// </summary>
    /// <param name="vrchatUserId">自分のVRChatユーザーID</param>
    /// <param name="myLocation">自分が居た/居るインスタンスの情報</param>
    /// <returns>インスタンスのメンバー情報</returns>
    public List<InstanceMember> GetInstanceMembers(string vrchatUserId, MyLocation myLocation)
    {
        Console.WriteLine($"VRCXDatabase.GetInstanceMembers(): {vrchatUserId}, {FormatDateTime(myLocation.JoinCreatedAt)}, {FormatDateTime(myLocation.EstimatedLeaveCreatedAt)}");
        var sanitizedUserId = vrchatUserId.Replace("_", "").Replace("-", "");
        var friendTableName = sanitizedUserId + "_friend_log_current";
        var sql = GetEmbedFileContent("VRCXDiscordTracker.Core.VRCX.Queries.instanceMembers.sql").Replace("@{friendTableName}", friendTableName);

        var instanceMembers = new List<InstanceMember>();
        using (var cmd = new SQLiteCommand(_conn))
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue(":join_created_at", FormatDateTime(myLocation.JoinCreatedAt.AddSeconds(-1)));
            cmd.Parameters.AddWithValue(":estimated_leave_created_at", FormatDateTime(myLocation.EstimatedLeaveCreatedAt?.AddSeconds(1)));
            cmd.Parameters.AddWithValue(":location", myLocation.LocationId);

            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // 取得したデータを処理
                var userId = reader.GetString(0);
                var displayName = reader.GetString(1);
                DateTime? lastJoinAt = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture);
                DateTime? lastLeaveAt = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture);
                var isCurrently = reader.GetBoolean(4);
                var isInstanceOwner = reader.GetBoolean(5);
                var isFriend = reader.GetBoolean(6);

                var instanceMember = new InstanceMember
                {
                    UserId = userId,
                    DisplayName = displayName,
                    LastJoinAt = lastJoinAt,
                    LastLeaveAt = lastLeaveAt,
                    IsCurrently = isCurrently,
                    IsInstanceOwner = isInstanceOwner,
                    IsFriend = isFriend,
                };
                instanceMembers.Add(instanceMember);
            }
        }

        return instanceMembers;
    }

    /// <summary>
    /// 埋め込まれたSQLファイルの内容を取得する
    /// </summary>
    /// <param name="name">SQLファイルの名前</param>
    /// <returns>SQLファイルの内容</returns>
    /// <exception cref="Exception">対象のリソースが見つからない場合</exception>
    private static string GetEmbedFileContent(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(name) ?? throw new Exception(
                $"Resource '{name}' not found in assembly '{assembly.FullName}'."
            );
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static string? FormatDateTime(DateTime? dateTime) => dateTime?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
}