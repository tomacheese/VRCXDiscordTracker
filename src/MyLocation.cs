using System;

namespace VRCXDiscordTracker
{
    internal class MyLocation
    {
        public long JoinId { get; set; }
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string Location { get; set; }
        public DateTime JoinCreatedAt { get; set; }
        public long JoinTime { get; set; }
        public long? LeaveId { get; set; }
        public DateTime? LeaveCreatedAt { get; set; }
        public long? LeaveTime { get; set; }
        public DateTime? NextJoinCreatedAt { get; set; }
        public DateTime? EstimatedLeaveCreatedAt { get; set; }
        public string WorldName { get; set; }
        public string WorldId { get; set; }
    }
}
