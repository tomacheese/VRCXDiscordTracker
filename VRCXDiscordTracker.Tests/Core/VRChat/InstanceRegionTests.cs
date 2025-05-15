using VRCXDiscordTracker.Core.VRChat;

namespace VRCXDiscordTracker.Tests.Core.VRChat;

/// <summary>
/// InstanceRegion.csのユニットテスト
/// </summary>
public class InstanceRegionTests
{
    /// <summary>
    /// USWest, USEast, Europe, Japanの定数が正しく生成されていることを検証
    /// </summary>
    [Fact]
    public void StaticFields_AreCorrect()
    {
        Assert.Equal(1, InstanceRegion.USWest.Id);
        Assert.Equal("us", InstanceRegion.USWest.Token);
        Assert.Equal("US West", InstanceRegion.USWest.Name);

        Assert.Equal(2, InstanceRegion.USEast.Id);
        Assert.Equal("use", InstanceRegion.USEast.Token);
        Assert.Equal("US East", InstanceRegion.USEast.Name);

        Assert.Equal(3, InstanceRegion.Europe.Id);
        Assert.Equal("eu", InstanceRegion.Europe.Token);
        Assert.Equal("Europe", InstanceRegion.Europe.Name);

        Assert.Equal(4, InstanceRegion.Japan.Id);
        Assert.Equal("jp", InstanceRegion.Japan.Token);
        Assert.Equal("Japan", InstanceRegion.Japan.Name);
    }

    /// <summary>
    /// GetAllで全てのInstanceRegionが取得できることを検証
    /// </summary>
    [Fact]
    public void GetAll_ReturnsAllRegions()
    {
        var all = InstanceRegion.GetAll<InstanceRegion>().ToList();
        Assert.Contains(InstanceRegion.USWest, all);
        Assert.Contains(InstanceRegion.USEast, all);
        Assert.Contains(InstanceRegion.Europe, all);
        Assert.Contains(InstanceRegion.Japan, all);
        Assert.Equal(4, all.Count);
    }

    /// <summary>
    /// GetByTokenでトークンから正しいInstanceRegionが取得できることを検証
    /// </summary>
    [Theory]
    [InlineData("us", "US West")]
    [InlineData("use", "US East")]
    [InlineData("eu", "Europe")]
    [InlineData("jp", "Japan")]
    [InlineData("US", "US West")]
    [InlineData("JP", "Japan")]
    public void GetByToken_ReturnsCorrectRegion(string token, string expectedName)
    {
        var region = InstanceRegion.GetByToken(token);
        Assert.NotNull(region);
        Assert.Equal(expectedName, region!.Name);
    }

    /// <summary>
    /// GetByTokenで存在しないトークンを指定した場合nullが返ることを検証
    /// </summary>
    [Fact]
    public void GetByToken_UnknownToken_ReturnsNull()
    {
        Assert.Null(InstanceRegion.GetByToken("unknown"));
        Assert.Null(InstanceRegion.GetByToken(""));
        Assert.Null(InstanceRegion.GetByToken(null));
    }

    /// <summary>
    /// CompareTo, Equals, GetHashCodeの動作を検証
    /// </summary>
    [Fact]
    public void CompareTo_Equals_GetHashCode_Works()
    {
        InstanceRegion a = InstanceRegion.USWest;
        InstanceRegion b = InstanceRegion.USWest;
        InstanceRegion c = InstanceRegion.Japan;

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
        InstanceRegion region = InstanceRegion.Japan;
        Assert.Equal("InstanceRegion(jp)", region.ToString(null, null));
        Assert.Equal("4", region.ToString("id", null));
        Assert.Equal("jp", region.ToString("token", null));
        Assert.Equal("Japan", region.ToString("name", null));
    }

    /// <summary>
    /// ToStringで未対応formatを指定した場合FormatExceptionが発生することを検証
    /// </summary>
    [Fact]
    public void ToString_UnsupportedFormat_Throws()
    {
        InstanceRegion region = InstanceRegion.Japan;
        Assert.Throws<FormatException>(() => region.ToString("xxx", null));
    }
}