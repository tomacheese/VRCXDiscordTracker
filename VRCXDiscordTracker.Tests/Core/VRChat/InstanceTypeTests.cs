using VRCXDiscordTracker.Core.VRChat;

namespace VRCXDiscordTracker.Tests.Core.VRChat;

/// <summary>
/// InstanceType.csのユニットテスト
/// </summary>
public class InstanceTypeTests
{
    /// <summary>
    /// Public, FriendsPlus, Friends, InvitePlus, Invite, GroupPublic, GroupPlus, Groupの定数が正しく生成されていることを検証
    /// </summary>
    [Fact]
    public void StaticFields_AreCorrect()
    {
        Assert.Equal(0, InstanceType.Public.Id);
        Assert.Equal("Public", InstanceType.Public.Name);

        Assert.Equal(1, InstanceType.FriendsPlus.Id);
        Assert.Equal("Friends+", InstanceType.FriendsPlus.Name);

        Assert.Equal(2, InstanceType.Friends.Id);
        Assert.Equal("Friends", InstanceType.Friends.Name);

        Assert.Equal(3, InstanceType.InvitePlus.Id);
        Assert.Equal("Invite+", InstanceType.InvitePlus.Name);

        Assert.Equal(4, InstanceType.Invite.Id);
        Assert.Equal("Invite", InstanceType.Invite.Name);

        Assert.Equal(5, InstanceType.GroupPublic.Id);
        Assert.Equal("Group Public", InstanceType.GroupPublic.Name);

        Assert.Equal(6, InstanceType.GroupPlus.Id);
        Assert.Equal("Group+", InstanceType.GroupPlus.Name);

        Assert.Equal(7, InstanceType.Group.Id);
        Assert.Equal("Group", InstanceType.Group.Name);
    }

    /// <summary>
    /// GetAllで全てのInstanceTypeが取得できることを検証
    /// </summary>
    [Fact]
    public void GetAll_ReturnsAllTypes()
    {
        var all = InstanceType.GetAll<InstanceType>().ToList();
        Assert.Contains(InstanceType.Public, all);
        Assert.Contains(InstanceType.FriendsPlus, all);
        Assert.Contains(InstanceType.Friends, all);
        Assert.Contains(InstanceType.InvitePlus, all);
        Assert.Contains(InstanceType.Invite, all);
        Assert.Contains(InstanceType.GroupPublic, all);
        Assert.Contains(InstanceType.GroupPlus, all);
        Assert.Contains(InstanceType.Group, all);
        Assert.Equal(8, all.Count);
    }

    /// <summary>
    /// CompareTo, Equals, GetHashCodeの動作を検証
    /// </summary>
    [Fact]
    public void CompareTo_Equals_GetHashCode_Works()
    {
        InstanceType a = InstanceType.Public;
        InstanceType b = InstanceType.Public;
        InstanceType c = InstanceType.Invite;

        Assert.Equal(0, a.CompareTo(b));
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        Assert.NotEqual(0, a.CompareTo(c));
        Assert.False(a.Equals(c));
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
        Assert.Equal(1, a.CompareTo(null));
    }

    /// <summary>
    /// ToStringのformat引数による動作を検証
    /// </summary>
    [Fact]
    public void ToString_Format_Works()
    {
        InstanceType type = InstanceType.GroupPlus;
        Assert.Equal("Group+", type.ToString(null, null));
        Assert.Equal("6", type.ToString("id", null));
        Assert.Equal("Group+", type.ToString("name", null));
    }

    /// <summary>
    /// ToStringで未対応formatを指定した場合FormatExceptionが発生することを検証
    /// </summary>
    [Fact]
    public void ToString_UnsupportedFormat_Throws()
    {
        InstanceType type = InstanceType.Group;
        Assert.Throws<FormatException>(() => type.ToString("xxx", null));
    }
}