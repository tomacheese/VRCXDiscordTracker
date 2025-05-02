namespace VRCXDiscordTracker.Core.VRCX;
internal class InstanceMember
{
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }
    public required DateTime LastJoinAt { get; set; }
    public required DateTime? LastLeaveAt { get; set; }
    public required bool IsCurrently { get; set; }
    public required bool IsInstanceOwner { get; set; }
    public required bool IsFriend { get; set; }
}
