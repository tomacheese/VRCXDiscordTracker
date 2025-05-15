using VRCXDiscordTracker.Core.VRChat;

namespace VRCXDiscordTracker.Tests.Core.VRChat;

/// <summary>
/// LocationParser.csのユニットテスト
/// </summary>
public class LocationParserTests
{
    /// <summary>
    /// 正常系: 最小限のロケーションID "wrld_xxxx:instanceId" をParseし、各プロパティが正しくセットされることを検証
    /// </summary>
    [Fact]
    public void Parse_MinimalLocationId_SetsPropertiesCorrectly()
    {
        // Arrange
        var locationId = "wrld_12345678:12345";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_12345678", result.WorldId);
        Assert.Equal("12345", result.InstanceName);
        Assert.Equal(InstanceType.Public, result.Type);
        Assert.Null(result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: region(jp)トークン付きロケーションIDをParseし、RegionがJPになることを検証
    /// </summary>
    [Fact]
    public void Parse_RegionToken_SetsRegionJP()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~region(jp)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.Public, result.Type);
        Assert.Null(result.OwnerId);
        Assert.Equal(InstanceRegion.Japan, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: group(grp_12345)トークン付きロケーションIDをParseし、GroupIdとTypeが正しくセットされることを検証
    /// </summary>
    [Fact]
    public void Parse_GroupToken_SetsGroupIdAndType()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~group(grp_12345)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.Group, result.Type);
        Assert.Equal("grp_12345", result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: group(grp_12345)~groupAccessType(members)トークン付きロケーションIDをParseし、TypeがGroup、GroupIdが正しくセットされることを検証
    /// </summary>
    [Fact]
    public void Parse_GroupAccessTypeMembers_SetsTypeGroup()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~group(grp_12345)~groupAccessType(members)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.Group, result.Type);
        Assert.Equal("grp_12345", result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: hidden(usr_xxx)トークン付きロケーションIDをParseし、TypeがFriendsPlus、OwnerIdがusr_xxxとなることを検証
    /// </summary>
    [Fact]
    public void Parse_HiddenToken_SetsTypeFriendsPlusAndOwnerId()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~hidden(usr_0b83d9be-9852-42dd-98e2-625062400acc)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.FriendsPlus, result.Type);
        Assert.Equal("usr_0b83d9be-9852-42dd-98e2-625062400acc", result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: friends(usr_xxx)トークン付きロケーションIDをParseし、TypeがFriends、OwnerIdがusr_xxxとなることを検証
    /// </summary>
    [Fact]
    public void Parse_FriendsToken_SetsTypeFriendsAndOwnerId()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~friends(usr_0b83d9be-9852-42dd-98e2-625062400acc)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.Friends, result.Type);
        Assert.Equal("usr_0b83d9be-9852-42dd-98e2-625062400acc", result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: private(usr_xxx)トークン付きロケーションIDをParseし、TypeがInvite、OwnerIdがusr_xxxとなることを検証
    /// </summary>
    [Fact]
    public void Parse_PrivateToken_SetsTypeInviteAndOwnerId()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~private(usr_0b83d9be-9852-42dd-98e2-625062400acc)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.Invite, result.Type);
        Assert.Equal("usr_0b83d9be-9852-42dd-98e2-625062400acc", result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: private(usr_xxx)~canRequestInviteトークン付きロケーションIDをParseし、TypeがInvitePlus、OwnerIdがusr_xxxとなることを検証
    /// </summary>
    [Fact]
    public void Parse_PrivateCanRequestInviteToken_SetsTypeInvitePlusAndOwnerId()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~private(usr_0b83d9be-9852-42dd-98e2-625062400acc)~canRequestInvite";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.InvitePlus, result.Type);
        Assert.Equal("usr_0b83d9be-9852-42dd-98e2-625062400acc", result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Null(result.Nonce);
    }

    /// <summary>
    /// 正常系: nonce(abcdefg)トークン付きロケーションIDをParseし、Nonceが正しくセットされることを検証
    /// </summary>
    [Fact]
    public void Parse_NonceToken_SetsNonce()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~nonce(abcdefg)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.Public, result.Type);
        Assert.Null(result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Equal("abcdefg", result.Nonce);
    }

    /// <summary>
    /// 正常系: region, group, groupAccessType, canRequestInvite, hidden, nonce混在の複合ロケーションIDをParseし、各プロパティが正しくセットされることを検証
    /// </summary>
    [Fact]
    public void Parse_MultipleTokens_SetsAllProperties()
    {
        // Arrange
        var locationId = "wrld_abcdef12:67890~region(us)~group(grp_12345)~groupAccessType(public)~canRequestInvite~hidden(usr_0b83d9be-9852-42dd-98e2-625062400acc)~nonce(abcdefg)";

        // Act
        VRChatInstance result = LocationParser.Parse(locationId);

        // Assert
        Assert.Equal("wrld_abcdef12", result.WorldId);
        Assert.Equal("67890", result.InstanceName);
        Assert.Equal(InstanceType.GroupPublic, result.Type);
        Assert.Equal("usr_0b83d9be-9852-42dd-98e2-625062400acc", result.OwnerId);
        Assert.Equal(InstanceRegion.USWest, result.Region);
        Assert.Equal("abcdefg", result.Nonce);
    }

    /// <summary>
    /// 異常系: locationIdがnull, 空文字, 空白のみの場合にArgumentExceptionが発生することを検証
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrEmpty_ThrowsArgumentException(string? locationId) => Assert.Throws<ArgumentException>(() => LocationParser.Parse(locationId!));

    /// <summary>
    /// 異常系: "local:", "offline:", "traveling:"で始まる場合にFormatExceptionが発生することを検証
    /// </summary>
    [Theory]
    [InlineData("local:xxxx")]
    [InlineData("offline:xxxx")]
    [InlineData("traveling:xxxx")]
    public void Parse_UnsupportedPrefix_ThrowsFormatException(string locationId) => Assert.Throws<FormatException>(() => LocationParser.Parse(locationId));

    /// <summary>
    /// 異常系: 不正なロケーションID形式の場合にFormatExceptionが発生することを検証
    /// </summary>
    [Theory]
    [InlineData("invalidstring")]
    [InlineData("wrld_xxxx")]
    [InlineData("wrld_xxxx:")]
    public void Parse_InvalidFormat_ThrowsFormatException(string locationId) => Assert.Throws<FormatException>(() => LocationParser.Parse(locationId));
}