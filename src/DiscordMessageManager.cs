using Discord;
using Discord.Rest;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace VRCXDiscordTracker
{
    internal class DiscordMessageManager
    {
        private static readonly string SaveFilePath = "discord-messages.json";
        private static readonly Dictionary<string, ulong> joinIdMessageIdPairs = LoadJoinIdMessageIdPairs();
        private static readonly Dictionary<ulong, string> lastMessageContent = new Dictionary<ulong, string>();

        private readonly MyLocation myLocation;
        private readonly List<InstanceMember> instanceMembers;

        public DiscordMessageManager(MyLocation myLocation, List<InstanceMember> instanceMembers)
        {
            this.myLocation = myLocation;
            this.instanceMembers = instanceMembers;
        }

        public async Task SendUpdateMessageAsync()
        {
            string joinId = GetJoinId();
            ulong? messageId = joinIdMessageIdPairs.ContainsKey(joinId) ? (ulong?)joinIdMessageIdPairs[joinId] : null;

            var embed = GetEmbed();

            if (messageId != null)
            {
                var updateResult = await UpdateMessage((ulong)messageId, embed);
                if (updateResult)
                {
                    return;
                }
            }

            ulong? newMessageId = await SendNewMessage(embed);
            if (newMessageId == null)
            {
                throw new Exception("Failed to send new message.");
            }

            joinIdMessageIdPairs[joinId] = (ulong)newMessageId;
            SaveJoinIdMessageIdPairs();
        }

        private async Task<ulong?> SendNewMessage(Embed embed)
        {
            var url = AppConfig.DiscordWebhookUrl;
            if (string.IsNullOrEmpty(url)) return null;

            var version = Assembly.GetExecutingAssembly().GetName().Version;

            using (var client = new DiscordWebhookClient(url))
            {
                return await client.SendMessageAsync(text: "", embeds: new[] { embed });
            }
        }

        private async Task<bool> UpdateMessage(ulong messageId, Embed embed)
        {
            var url = AppConfig.DiscordWebhookUrl;
            if (string.IsNullOrEmpty(url)) return false;

            // 最後に投稿した内容と同じであれば何もしない
            if (lastMessageContent.ContainsKey(messageId) && lastMessageContent[messageId] == embed.ToString())
            {
                return true;
            }

            using (var client = new DiscordWebhookClient(url))
            {
                try
                {
                    await client.ModifyMessageAsync(messageId, m => m.Embeds = new[] { embed });
                    lastMessageContent[messageId] = embed.ToJsonString();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating message: {ex.Message}");
                    return false;
                }
            }
        }

        private static Dictionary<string, ulong> LoadJoinIdMessageIdPairs()
        {
            if (!File.Exists(SaveFilePath))
            {
                return new Dictionary<string, ulong>();
            }

            try
            {
                var json = File.ReadAllText(SaveFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, ulong>>(json) ?? new Dictionary<string, ulong>();
            }
            catch
            {
                return new Dictionary<string, ulong>();
            }
        }

        private static void SaveJoinIdMessageIdPairs()
        {
            try
            {
                var json = JsonSerializer.Serialize(joinIdMessageIdPairs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving joinIdMessageIdPairs: {ex.Message}");
            }
        }
        private Embed GetEmbed()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;

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
                    Name = myLocation.DisplayName,
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

        private string FormatDateTime(DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return "";
            }
            return ((DateTime)dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.CreateSpecificCulture("ja-JP"));
        }

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

        private string GetMembersString(List<InstanceMember> members)
        {
            return string.Join("\n", members.ConvertAll(member => $"{GetMemberEmoji(member)} [{member.DisplayName}](https://vrchat.com/home/user/{member.UserId}): {FormatDateTime(member.LastJoinAt)} - {FormatDateTime(member.LastLeaveAt)}"));
        }
    }
}
