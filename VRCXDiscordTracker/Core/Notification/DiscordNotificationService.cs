using Discord;
using Discord.Webhook;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using VRCXDiscordTracker.Core.Config;
using VRCXDiscordTracker.Core.VRCX;
using Color = Discord.Color;
using Format = Discord.Format;

namespace VRCXDiscordTracker.Core.Notification;
internal class DiscordNotificationService(MyLocation myLocation, List<InstanceMember> instanceMembers)
{

    private static readonly string _saveFilePath = "discord-messages.json";
    private static readonly Dictionary<string, ulong> _joinIdMessageIdPairs = LoadJoinIdMessageIdPairs();
    private static readonly Dictionary<ulong, Embed> _lastMessageContent = [];

    /// <summary>
    /// JSONのシリアルオプション
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task SendUpdateMessageAsync()
    {
        string joinId = GetJoinId();
        ulong? messageId = _joinIdMessageIdPairs.TryGetValue(joinId, out ulong value) ? (ulong?)value : null;

        var embed = GetEmbed();

        if (messageId != null)
        {
            var updateResult = await UpdateMessage((ulong)messageId, embed);
            if (updateResult)
            {
                return;
            }
        }

        ulong? newMessageId = await SendNewMessage(embed) ?? throw new Exception("Failed to send new message. Webhook URL is empty.");
        _joinIdMessageIdPairs[joinId] = (ulong)newMessageId;
        SaveJoinIdMessageIdPairs();
    }

    private static async Task<ulong?> SendNewMessage(Embed embed)
    {
        var url = AppConfig.DiscordWebhookUrl;
        if (string.IsNullOrEmpty(url)) return null;

        using var client = new DiscordWebhookClient(url);
        return await client.SendMessageAsync(text: string.Empty, embeds: [embed]);
    }

    private static async Task<bool> UpdateMessage(ulong messageId, Embed embed)
    {
        var url = AppConfig.DiscordWebhookUrl;
        if (string.IsNullOrEmpty(url)) return false;

        // 最後に投稿した内容と同じであれば何もしない
        if (_lastMessageContent.TryGetValue(messageId, out Embed? value) && EqualEmbedWithoutTimestamp(value, embed))
        {
            return true;
        }

        using var client = new DiscordWebhookClient(url);
        try
        {
            await client.ModifyMessageAsync(messageId, m => m.Embeds = new[] { embed });
            _lastMessageContent[messageId] = embed;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating message: {ex.Message}");
            return false;
        }
    }

    private static Dictionary<string, ulong> LoadJoinIdMessageIdPairs()
    {
        if (!File.Exists(_saveFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_saveFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, ulong>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void SaveJoinIdMessageIdPairs()
    {
        try
        {
            var json = JsonSerializer.Serialize(_joinIdMessageIdPairs, _jsonSerializerOptions);
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving joinIdMessageIdPairs: {ex.Message}");
        }
    }
    private Embed GetEmbed()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

        // インスタンスIDは、Locationの:よりあと
        var locationParts = myLocation.Location.Split(':');
        if (locationParts.Length != 2)
        {
            throw new FormatException("Location string is not in the expected format with a colon.");
        }
        var instanceId = locationParts[1];
        var embed = new EmbedBuilder
        {
            Title = myLocation.WorldName,
            Url = $"https://vrchat.com/home/launch?worldId={myLocation.WorldId}&instanceId={instanceId}",
            Author = new EmbedAuthorBuilder
            {
                Name = Format.Sanitize(myLocation.DisplayName),
            },
            Timestamp = DateTime.UtcNow,
            Footer = new EmbedFooterBuilder
            {
                Text = $"VRCXDiscordTracker {version.Major}.{version.Minor}.{version.Build}",
            }
        };

        // 自身が IsCurrently=true の場合、色は緑。そうではない場合は黄色
        var isCurrently = instanceMembers.Exists(member => member.UserId == myLocation.UserId && member.IsCurrently);
        embed.Color = isCurrently ? Color.Green : new Color(0xFFFF00);

        // isCurrently=true のメンバーは、CurrentMembers として表示する
        // フォーマットは "{emoji} [{member.DisplayName}](https://vrchat.com/home/user/{member.UserId})]: {member.LastJoinAt} - {member.LastLeaveAt}"
        var currentMembers = instanceMembers.FindAll(member => member.IsCurrently);
        if (currentMembers.Count > 0)
        {
            embed.AddField(new EmbedFieldBuilder
            {
                Name = "Current Members",
                Value = GetMembersString(currentMembers),
                IsInline = false
            });
        }

        var pastMembers = instanceMembers.FindAll(member => !member.IsCurrently);
        if (pastMembers.Count > 0)
        {
            embed.AddField(new EmbedFieldBuilder
            {
                Name = "Past Members",
                Value = GetMembersString(pastMembers),
                IsInline = false
            });
        }

        return embed.Build();
    }

    private string GetJoinId()
    {
        return myLocation.JoinId.ToString();
    }

    private static bool EqualEmbedWithoutTimestamp(Embed left, Embed right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        var leftWithoutTimestamp = left.ToEmbedBuilder();
        leftWithoutTimestamp.Timestamp = null;
        var rightWithoutTimestamp = right.ToEmbedBuilder();
        rightWithoutTimestamp.Timestamp = null;

        return leftWithoutTimestamp.Equals(rightWithoutTimestamp);
    }
    private static string FormatDateTime(DateTime? dateTime) => dateTime?.ToString("G", CultureInfo.CurrentCulture) ?? string.Empty;

    private string GetMemberEmoji(InstanceMember member)
    {
        // インスタンスオーナーの場合は "👑"
        if (member.IsInstanceOwner)
        {
            return "👑";
        }

        // 自分自身の場合は "👤"
        if (member.UserId == myLocation.UserId)
        {
            return "👤";
        }

        // フレンドの場合は "⭐️"
        if (member.IsFriend)
        {
            return "⭐️";
        }

        // それ以外は "⬜️"
        return "⬜️";
    }

    private string GetMembersString(List<InstanceMember> members, bool includeJoinLeaveAt = true, bool includeUserPageLink = true)
    {
        var result = string.Join("\n", members.ConvertAll(member =>
        {
            var baseText = $"{GetMemberEmoji(member)} ";
            var escapedName = Format.Sanitize(member.DisplayName);

            // includeUserPageLink が true の場合は、ユーザーページのリンクを追加する
            if (includeUserPageLink)
            {
                baseText += $"[{escapedName}](https://vrchat.com/home/user/{member.UserId})";
            }
            else
            {
                baseText += escapedName;
            }

            // includeJoinLeaveAt が true の場合は、JoinAt と LeaveAt を追加する
            if (includeJoinLeaveAt)
            {
                baseText += $": {FormatDateTime(member.LastJoinAt)} - {FormatDateTime(member.LastLeaveAt)}";
            }

            return baseText;
        }));

        if (result.Length >= 1000 && includeJoinLeaveAt && includeUserPageLink)
        {
            // 1000文字を超える場合は、JoinLeaveAtを省略する
            result = GetMembersString(members, false, true);
        }
        if (result.Length >= 1000)
        {
            // 1000文字を超える場合は、ユーザーページのリンクを省略する
            result = GetMembersString(members, includeJoinLeaveAt, false);
        }

        // それでも1000文字を超える場合は、メッセージを切り落とす
        return result.Length > 1000 ? string.Concat(result.AsSpan(0, 1000), "...") : result;
    }
}
