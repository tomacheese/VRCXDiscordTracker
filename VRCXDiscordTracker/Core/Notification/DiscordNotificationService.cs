using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Discord;
using Discord.Webhook;
using VRCXDiscordTracker.Core.Config;
using VRCXDiscordTracker.Core.VRCX;
using Color = Discord.Color;
using Format = Discord.Format;

namespace VRCXDiscordTracker.Core.Notification;

/// <summary>
/// DiscordのWebhookを使用して、VRCXのインスタンス情報を通知するサービス
/// </summary>
/// <param name="myLocation">自分が居た/居るインスタンスの情報</param>
/// <param name="instanceMembers">インスタンスのメンバー情報</param>
internal class DiscordNotificationService(MyLocation myLocation, List<InstanceMember> instanceMembers)
{
    /// <summary>
    /// 保存パス
    /// </summary>
    private static readonly string _saveFilePath = "discord-messages.json";

    /// <summary>
    /// JoinIdとMessageIdのペアを保存する辞書
    /// </summary>
    private static readonly Dictionary<string, ulong> _joinIdMessageIdPairs = LoadJoinIdMessageIdPairs();

    /// <summary>
    /// 最後に投稿したメッセージの内容を保存する辞書
    /// </summary>
    private static readonly Dictionary<ulong, Embed> _lastMessageContent = [];

    /// <summary>
    /// JSONのシリアルオプション
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// DIscordにメッセージを送信、もしくは更新する
    /// </summary>
    /// <returns>タスク</returns>
    /// <exception cref="Exception">Webhook URLが空の場合</exception>
    public async Task SendUpdateMessageAsync()
    {
        Console.WriteLine("DiscordNotificationService.SendUpdateMessageAsync()");
        var joinId = GetJoinId();
        var messageId = _joinIdMessageIdPairs.TryGetValue(joinId, out var value) ? (ulong?)value : null;

        Embed embed = GetEmbed();

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

    /// <summary>
    /// DiscordのWebhookを使用して新しいメッセージを送信する
    /// </summary>
    /// <param name="embed">メッセージのEmbed</param>
    /// <returns>メッセージIDを含むTask</returns>
    private static async Task<ulong?> SendNewMessage(Embed embed)
    {
        var url = AppConfig.DiscordWebhookUrl;
        if (string.IsNullOrEmpty(url)) return null;

        using var client = new DiscordWebhookClient(url);
        return await client.SendMessageAsync(text: string.Empty, embeds: [embed]);
    }

    /// <summary>
    /// DiscordのWebhookを使用してメッセージを更新する
    /// </summary>
    /// <param name="messageId">メッセージID</param>
    /// <param name="embed">メッセージのEmbed</param>
    /// <returns>更新が成功した場合はtrue、失敗した場合はfalseを含むTask</returns>
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

    /// <summary>
    /// JoinIdとMessageIdのペアを保存する辞書をロードする
    /// </summary>
    /// <returns>JoinIdとMessageIdのペアを保存する辞書</returns>
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

    /// <summary>
    /// JoinIdとMessageIdのペアを保存する辞書を保存する
    /// </summary>
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

    /// <summary>
    /// Embedを取得する
    /// </summary>
    /// <returns>Embed</returns>
    /// <exception cref="FormatException">Location文字列がコロンで区切られていない場合</exception>
    private Embed GetEmbed()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

        // インスタンスIDは、Locationの:よりあと
        var locationParts = myLocation.LocationId.Split(':');
        if (locationParts.Length != 2)
        {
            throw new FormatException("Location string is not in the expected format with a colon.");
        }
        var instanceId = locationParts[1];
        var embed = new EmbedBuilder
        {
            Title = $"{myLocation.WorldName} ({myLocation.LocationInstance.Type})",
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
        List<InstanceMember> currentMembers = instanceMembers.FindAll(member => member.IsCurrently);
        if (currentMembers.Count > 0)
        {
            embed.AddField(new EmbedFieldBuilder
            {
                Name = "Current Members",
                Value = GetMembersString(currentMembers),
                IsInline = false
            });
        }

        List<InstanceMember> pastMembers = instanceMembers.FindAll(member => !member.IsCurrently);
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

    /// <summary>
    /// JoinIdを取得する
    /// </summary>
    /// <returns></returns>
    private string GetJoinId() => myLocation.JoinId.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// タイムスタンプを除いたEmbedの等価性を比較する
    /// </summary>
    /// <param name="left">右側のEmbed</param>
    /// <param name="right">左側のEmbed</param>
    /// <returns>タイムスタンプを除いたEmbedが等しい場合はtrue、そうでない場合はfalse</returns>
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

    /// <summary>
    /// DateTimeをフォーマットする
    /// </summary>
    /// <param name="dateTime">フォーマットするDateTime</param>
    /// <returns>フォーマットされたDateTime文字列</returns>
    private static string FormatDateTime(DateTime? dateTime) => dateTime?.ToString("G", CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>
    /// メンバーの絵文字を取得する。インスタンスオーナー、自分自身、フレンド、それ以外で絵文字を設定する。
    /// </summary>
    /// <param name="member">対象のメンバー</param>
    /// <returns>メンバーの絵文字</returns>
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

    /// <summary>
    /// メンバーの情報をリスト化された文字列として取得する
    /// </summary>
    /// <param name="members">メンバーのリスト</param>
    /// <param name="includeJoinLeaveAt">参加・退出時間を含めるかどうか</param>
    /// <param name="includeUserPageLink">ユーザーページのリンクを含めるかどうか</param>
    /// <returns>リスト化されたメンバー情報</returns>
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
                // LastLeaveAt が LastJoinAt より後の場合に値を代入し、それ以外は null
                DateTime? lastLeaveAt = member.LastLeaveAt > member.LastJoinAt ? member.LastLeaveAt : null;
                baseText += $": {FormatDateTime(member.LastJoinAt)} - {FormatDateTime(lastLeaveAt)}";
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