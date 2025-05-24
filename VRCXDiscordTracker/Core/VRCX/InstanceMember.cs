namespace VRCXDiscordTracker.Core.VRCX;

/// <summary>
/// VRCXのインスタンスメンバーを表すクラス
/// </summary>
internal class InstanceMember
{
    /// <summary>
    /// インスタンスメンバーのID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// インスタンスメンバーの名前
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// 最終参加日時
    /// </summary>
    public required DateTime? LastJoinAt { get; set; }

    /// <summary>
    /// 最終退出日時
    /// </summary>
    public required DateTime? LastLeaveAt { get; set; }

    /// <summary>
    /// 現在もインスタンスに参加しているかどうか
    /// </summary>
    public required bool IsCurrently { get; set; }

    /// <summary>
    /// インスタンスのオーナーかどうか
    /// </summary>
    public required bool IsInstanceOwner { get; set; }

    /// <summary>
    /// フレンドかどうか
    /// </summary>
    public required bool IsFriend { get; set; }
}
