namespace VRCXDiscordTracker.Core.VRCX;

/// <summary>
/// 自分が居た/居るインスタンスの情報を格納するクラス
/// </summary>
internal class MyLocation
{
    /// <summary>
    /// 参加ID
    /// </summary>
    public required long JoinId { get; set; }

    /// <summary>
    /// ユーザーID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// ユーザー名
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// ロケーションID
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// インスタンスに参加した日時
    /// </summary>
    public required DateTime JoinCreatedAt { get; set; }

    /// <summary>
    /// JoinイベントのTime値。基本的に0になる。
    /// </summary>
    public required long JoinTime { get; set; }

    /// <summary>
    /// 退出ID
    /// </summary>
    public required long? LeaveId { get; set; }

    /// <summary>
    /// 退出日時
    /// </summary>
    public required DateTime? LeaveCreatedAt { get; set; }

    /// <summary>
    /// 退出イベントのTime値。インスタンスに居た時間 (ミリ秒)
    /// </summary>
    public required long? LeaveTime { get; set; }

    /// <summary>
    /// 次に異なるインスタンスに参加した日時
    /// </summary>
    public required DateTime? NextJoinCreatedAt { get; set; }

    /// <summary>
    /// おそらくこのインスタンスを退出した日時
    /// </summary>
    public required DateTime? EstimatedLeaveCreatedAt { get; set; }

    /// <summary>
    /// ワールドの名前
    /// </summary>
    public required string? WorldName { get; set; }

    /// <summary>
    /// ワールドのID
    /// </summary>
    public required string? WorldId { get; set; }
}
