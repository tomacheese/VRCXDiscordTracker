using Discord;
using VRCXDiscordTracker.Core.Notification;

namespace VRCXDiscordTracker.Tests.Core.Notification;

/// <summary>
/// DiscordEmbedMembers.ReduceFieldsの境界値・異常系テスト
/// </summary>
public class DiscordEmbedMembersReduceFieldsTests
{
    /// <summary>
    /// ReduceFields: 25件超過時は25件にトリムされる
    /// </summary>
    [Fact]
    public void ReduceFields_Over25Fields_TrimmedTo25()
    {
        var embed = new EmbedBuilder { Title = "test" };
        var fields = new List<EmbedFieldBuilder>();
        for (var i = 0; i < 30; i++)
        {
            fields.Add(new EmbedFieldBuilder { Name = $"F{i}", Value = "a" });
        }
        List<EmbedFieldBuilder> reduced = DiscordEmbedMembers.ReduceFields(embed, fields);
        Assert.Equal(25, reduced.Count);
    }

    /// <summary>
    /// ReduceFields: フィールド数が25を超える場合、25件以内にトリムされる
    /// </summary>
    [Fact]
    public void ReduceFields_FieldCountOver25_TrimmedTo25()
    {
        var embed = new EmbedBuilder { Title = "test" };
        var fields = new List<EmbedFieldBuilder>();
        for (var i = 0; i < 30; i++)
        {
            fields.Add(new EmbedFieldBuilder { Name = $"F{i}", Value = "a" });
        }
        List<EmbedFieldBuilder> reduced = DiscordEmbedMembers.ReduceFields(embed, fields);
        Assert.True(reduced.Count <= 25);
    }
}