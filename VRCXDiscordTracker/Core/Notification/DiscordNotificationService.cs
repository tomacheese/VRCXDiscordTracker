using System.Globalization;
using System.Text.Json;
using Discord;
using Discord.Webhook;
using VRCXDiscordTracker.Core.Config;
using VRCXDiscordTracker.Core.VRCX;

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

        Embed embed = new DiscordEmbedMembers(myLocation, instanceMembers).GetEmbed();

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
}