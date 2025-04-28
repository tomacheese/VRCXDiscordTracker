using System;

namespace VRCXDiscordTracker
{
    internal class InstanceMember
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public DateTime LastJoinAt { get; set; }
        public DateTime? LastLeaveAt { get; set; }
        public bool IsCurrently { get; set; }
        public bool IsInstanceOwner { get; set; }
        public bool IsFriend { get; set; }
    }
}
