using Discord;
using VRCXDiscordTracker.Core.Notification;
using VRCXDiscordTracker.Core.VRChat;
using VRCXDiscordTracker.Core.VRCX;

namespace VRCXDiscordTracker.Tests.Core.Notification;

/// <summary>
/// DiscordEmbedMembersのインスタンスメソッド単体テスト
/// </summary>
public class DiscordEmbedMembersInstanceTests
{
    private static MyLocation CreateLocation(string userId = "usr_test", string displayName = "TestUser", string locationId = "wrld_123:12345")
    {
        return new MyLocation
        {
            JoinId = 1,
            UserId = userId,
            DisplayName = displayName,
            LocationId = locationId,
            LocationInstance = new VRChatInstance
            {
                WorldId = "wrld_123",
                InstanceName = "12345",
                Type = InstanceType.Public,
                Region = InstanceRegion.Japan
            },
            JoinCreatedAt = DateTime.UtcNow,
            JoinTime = 0,
            LeaveId = null,
            LeaveCreatedAt = null,
            LeaveTime = null,
            NextJoinCreatedAt = null,
            EstimatedLeaveCreatedAt = null,
            WorldName = "TestWorld",
            WorldId = "wrld_123"
        };
    }

    private static InstanceMember CreateMember(string userId, string displayName, bool isCurrently = true, bool isOwner = false, bool isFriend = false)
    {
        return new InstanceMember
        {
            UserId = userId,
            DisplayName = displayName,
            LastJoinAt = DateTime.UtcNow.AddMinutes(-10),
            LastLeaveAt = isCurrently ? null : DateTime.UtcNow.AddMinutes(-5),
            IsCurrently = isCurrently,
            IsInstanceOwner = isOwner,
            IsFriend = isFriend
        };
    }

    /// <summary>
    /// GetBaseEmbed: 正常にEmbedBuilderが生成される
    /// </summary>
    [Fact]
    public void GetBaseEmbed_ValidInput_ReturnsEmbedBuilder()
    {
        MyLocation loc = CreateLocation();
        var members = new List<InstanceMember> { CreateMember("usr_test", "TestUser") };
        var dem = new DiscordEmbedMembers(loc, members);
        EmbedBuilder embed = dem.GetBaseEmbed();
        Assert.NotNull(embed);
        Assert.Contains("TestWorld", embed.Title);
        Assert.Contains("Current Members Count", embed.Description);
        Assert.Equal("TestUser", embed.Author.Name);
    }

    /// <summary>
    /// GetBaseEmbed: LocationIdが":"を含まない場合FormatException
    /// </summary>
    [Fact]
    public void GetBaseEmbed_LocationIdInvalid_ThrowsFormatException()
    {
        MyLocation loc = CreateLocation(locationId: "wrld_123");
        var members = new List<InstanceMember> { CreateMember("usr_test", "TestUser") };
        var dem = new DiscordEmbedMembers(loc, members);
        Assert.Throws<FormatException>(() => dem.GetBaseEmbed());
    }

    /// <summary>
    /// GetMemberFields: メンバー0件で空リスト
    /// </summary>
    [Fact]
    public void GetMemberFields_EmptyMembers_ReturnsEmptyList()
    {
        MyLocation loc = CreateLocation();
        var dem = new DiscordEmbedMembers(loc, []);
        List<EmbedFieldBuilder> result = dem.GetMemberFields(
            0, // Currently
            [],
            0 // Full
        );
        Assert.Empty(result);
    }

    /// <summary>
    /// GetMemberFields: フィールド分割される（MaxFieldValueLength超過）
    /// </summary>
    [Fact]
    public void GetMemberFields_FieldSplitByLength()
    {
        MyLocation loc = CreateLocation();
        var members = new List<InstanceMember>();
        for (var i = 0; i < 10; i++)
        {
            members.Add(CreateMember($"usr_{i}", new string('A', EmbedFieldBuilder.MaxFieldValueLength / 5)));
        }
        var dem = new DiscordEmbedMembers(loc, members);
        List<EmbedFieldBuilder> result = dem.GetMemberFields(
            0,
            members,
            0
        );
        Assert.True(result.Count > 1);
    }

    /// <summary>
    /// GetMembersString: 各MemberTextFormatで出力が変わる
    /// </summary>
    [Fact]
    public void GetMembersString_TextFormatVariants()
    {
        MyLocation loc = CreateLocation();
        var members = new List<InstanceMember>
        {
            CreateMember("usr_a", "A"),
            CreateMember("usr_b", "B", isCurrently: false)
        };
        var dem = new DiscordEmbedMembers(loc, members);
        var full = dem.GetMembersString(members, 0);
        var excludeLinks = dem.GetMembersString(members, (DiscordEmbedMembers.MemberTextFormat)1);
        var nameOnly = dem.GetMembersString(members, (DiscordEmbedMembers.MemberTextFormat)2);
        Assert.Contains("https://vrchat.com/home/user/", full);
        Assert.Contains(":", excludeLinks);
        Assert.DoesNotContain("https://", nameOnly);
    }

    /// <summary>
    /// GetMemberEmoji: オーナー/自分/フレンド/その他
    /// </summary>
    [Fact]
    public void GetMemberEmoji_AllPatterns()
    {
        MyLocation loc = CreateLocation("usr_test", "TestUser");
        var dem = new DiscordEmbedMembers(loc, []);
        Assert.Equal("👑", dem.GetMemberEmoji(CreateMember("usr_owner", "Owner", isOwner: true)));
        Assert.Equal("👤", dem.GetMemberEmoji(CreateMember("usr_test", "TestUser")));
        Assert.Equal("⭐️", dem.GetMemberEmoji(CreateMember("usr_friend", "Friend", isFriend: true)));
        Assert.Equal("⬜️", dem.GetMemberEmoji(CreateMember("usr_other", "Other")));
    }
}