namespace VRCXDiscordTracker.Core.VRCX;
internal class MyLocation
{
    public required long JoinId { get; set; }
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }
    public required string Location { get; set; }
    public required DateTime JoinCreatedAt { get; set; }
    public required long JoinTime { get; set; }
    public required long? LeaveId { get; set; }
    public required DateTime? LeaveCreatedAt { get; set; }
    public required long? LeaveTime { get; set; }
    public required DateTime? NextJoinCreatedAt { get; set; }
    public required DateTime? EstimatedLeaveCreatedAt { get; set; }
    public required string? WorldName { get; set; }
    public required string? WorldId { get; set; }
}
