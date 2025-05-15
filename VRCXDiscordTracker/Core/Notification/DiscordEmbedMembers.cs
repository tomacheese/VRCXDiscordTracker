using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Discord;
using VRCXDiscordTracker.Core.VRCX;
using Color = Discord.Color;

namespace VRCXDiscordTracker.Core.Notification;

/// <summary>
/// インスタンスおよびメンバー情報をもとに Discord 向け Embed を生成するクラス
/// </summary>
/// <param name="myLocation">現在のユーザーのロケーション情報</param>
/// <param name="instanceMembers">インスタンスのメンバーリスト</param>
internal partial class DiscordEmbedMembers(MyLocation myLocation, List<InstanceMember> instanceMembers)
{

    /// <summary>
    /// 連続するアンダースコアをエスケープする Regex を生成する
    /// </summary>
    /// <returns>アンダースコアをエスケープする Regex</returns>
    [GeneratedRegex(@"(?<!<a?:.+|https?:\/\/\S+)__(_)?(?!:\d+>)")]
    private static partial Regex SanitizeUnderscoreRegex();

    /// <summary>
    /// インスタンスメンバー情報から Discord Embed を構築する
    /// </summary>
    /// <returns>構築済み Embed</returns>
    public Embed GetEmbed()
    {
        Console.WriteLine($"GetEmbed started. Total members: {instanceMembers.Count}");
        EmbedBuilder baseEmbed = GetBaseEmbed();

        var baseEmbedLength = baseEmbed.Length;
        var remainingLength = EmbedFieldBuilder.MaxFieldValueLength - baseEmbedLength;

        var currentFieldTitle = "Current Members";
        List<InstanceMember> currentMembers = instanceMembers.FindAll(member => member.IsCurrently);
        Console.WriteLine($"CurrentMembers count: {currentMembers.Count}");
        List<EmbedFieldBuilder> currentFullFields = GetMemberFields(currentFieldTitle, currentMembers, MemberTextFormat.Full);
        List<EmbedFieldBuilder> currentExcludedLinksFields = GetMemberFields(currentFieldTitle, currentMembers, MemberTextFormat.ExcludeLinks);
        List<EmbedFieldBuilder> currentMinimumFields = GetMemberFields(currentFieldTitle, currentMembers, MemberTextFormat.NameOnly);

        var pastFieldTitle = "Past Members";
        List<InstanceMember> pastMembers = instanceMembers.FindAll(member => !member.IsCurrently);
        Console.WriteLine($"PastMembers count: {pastMembers.Count}");
        List<EmbedFieldBuilder> pastFullFields = GetMemberFields(pastFieldTitle, pastMembers, MemberTextFormat.Full);
        List<EmbedFieldBuilder> pastExcludedLinksFields = GetMemberFields(pastFieldTitle, pastMembers, MemberTextFormat.ExcludeLinks);
        List<EmbedFieldBuilder> pastMinimumFields = GetMemberFields(pastFieldTitle, pastMembers, MemberTextFormat.NameOnly);

        // フィールドパターン1: フルフォーマットのCurrent+Pastで
        var patternFields1 = currentFullFields.Concat(pastFullFields).ToList();
        Console.WriteLine($"Trying pattern1 with fields count: {patternFields1.Count}");
        EmbedBuilder pattern1 = SetFields(baseEmbed, patternFields1);
        if (ValidateEmbed(pattern1))
        {
            Console.WriteLine("Build pattern1");
            return pattern1.Build();
        }

        // フィールドパターン2: Currentはフル、Pastはリンク省略
        var patternFields2 = currentFullFields.Concat(pastExcludedLinksFields).ToList();
        EmbedBuilder pattern2 = SetFields(baseEmbed, patternFields2);
        if (ValidateEmbed(pattern2))
        {
            Console.WriteLine("Build pattern2");
            return pattern2.Build();
        }

        // フィールドパターン3: Currentはフル、Pastは最小限
        var patternFields3 = currentFullFields.Concat(pastMinimumFields).ToList();
        EmbedBuilder pattern3 = SetFields(baseEmbed, patternFields3);
        if (ValidateEmbed(pattern3))
        {
            Console.WriteLine("Build pattern3");
            return pattern3.Build();
        }

        // フィールドパターン4: Currentはリンク省略、Pastは最小限
        var patternFields4 = currentExcludedLinksFields.Concat(pastMinimumFields).ToList();
        EmbedBuilder pattern4 = SetFields(baseEmbed, patternFields4);
        if (ValidateEmbed(pattern4))
        {
            Console.WriteLine("Build pattern4");
            return pattern4.Build();
        }

        // フィールドパターン5: 両方とも最小限
        var patternFields5 = currentMinimumFields.Concat(pastMinimumFields).ToList();
        EmbedBuilder pattern5 = SetFields(baseEmbed, patternFields5);
        if (ValidateEmbed(pattern5))
        {
            Console.WriteLine("Build pattern5");
            return pattern5.Build();
        }

        // 最終パターン: 最小限フィールドをReduceFieldsでさらに文字数調整
        var patternFieldsLast = currentMinimumFields.Concat(pastMinimumFields).ToList();
        EmbedBuilder patternLast = baseEmbed.WithFields(ReduceFields(baseEmbed, patternFieldsLast));
        return patternLast.Build();
    }

    /// <summary>
    /// タイトル、説明、著者、フッター、色などの基本設定を持つ EmbedBuilder を生成する
    /// </summary>
    /// <returns>基本設定済み EmbedBuilder</returns>
    /// <exception cref="FormatException">ロケーション ID の形式不正</exception>
    private EmbedBuilder GetBaseEmbed()
    {
        Console.WriteLine($"GetBaseEmbed - World: {myLocation.WorldName}, Type: {myLocation.LocationInstance.Type}");
        Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

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
            Description = $"Current Members Count: {instanceMembers.Count(member => member.IsCurrently)}\n" +
                          $"Past Members Count: {instanceMembers.Count(member => !member.IsCurrently)}\n",
            Author = new EmbedAuthorBuilder
            {
                Name = Sanitize(myLocation.DisplayName),
            },
            Timestamp = DateTime.UtcNow,
            Footer = new EmbedFooterBuilder
            {
                Text = DiscordNotificationService.EmbedFooterText,
            }
        };

        var isCurrently = instanceMembers.Exists(member => member.UserId == myLocation.UserId && member.IsCurrently);
        embed.Color = isCurrently ? Color.Green : new Color(0xFFFF00);

        Console.WriteLine($"BaseEmbed Title: {embed.Title}");
        return embed;
    }

    /// <summary>
    /// EmbedBuilder の構築可否を検証する
    /// </summary>
    /// <param name="embed">検証対象 EmbedBuilder</param>
    /// <returns>有効なら true、例外発生時は false</returns>
    private static bool ValidateEmbed(EmbedBuilder embed)
    {
        try
        {
            embed.Build();
            Console.WriteLine("Embed validation succeeded");
            return true;
        }
        catch (Exception)
        {
            Console.WriteLine("Embed validation failed");
            return false;
        }
    }

    /// <summary>
    /// EmbedBuilder のフィールドを指定リストに置換する
    /// </summary>
    /// <param name="embed">対象 EmbedBuilder</param>
    /// <param name="fields">設定する EmbedFieldBuilder リスト</param>
    /// <returns>フィールド置換後の EmbedBuilder</returns>
    private static EmbedBuilder SetFields(EmbedBuilder embed, List<EmbedFieldBuilder> fields)
    {
        Console.WriteLine($"SetFields - setting {fields.Count} fields");
        embed.Fields.Clear();
        return embed.WithFields(fields);
    }

    /// <summary>
    /// テキスト内のアンダースコアを Discord 用にエスケープする
    /// </summary>
    /// <param name="text">対象テキスト</param>
    /// <returns>エスケープ後テキスト</returns>
    private static string Sanitize(string text)
    {
        var idx = 0;
        return SanitizeUnderscoreRegex().Replace(text, match =>
        {
            if (match.Groups[1].Success)
            {
                return ++idx % 2 == 1 ? $"{match.Groups[1].Value}\\_\\_" : $"\\_\\_{match.Groups[1].Value}";
            }
            return "\\_\\_";
        });
    }

    /// <summary>
    /// Nullable DateTime をローカルカルチャ形式の文字列に変換する
    /// </summary>
    /// <param name="dateTime">対象日時</param>
    /// <returns>フォーマット済み日時。null は空文字</returns>
    private static string FormatDateTime(DateTime? dateTime) => dateTime?.ToString("G", CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>
    /// Embed のサイズ制限内に収まるようフィールド数・文字数を削減する
    /// </summary>
    /// <param name="baseEmbed">基本 EmbedBuilder</param>
    /// <param name="fields">初期 EmbedFieldBuilder リスト</param>
    /// <returns>制限内調整済み EmbedFieldBuilder リスト</returns>
    /// <exception cref="Exception">サイズ制限超過時</exception>
    private static List<EmbedFieldBuilder> ReduceFields(EmbedBuilder baseEmbed, List<EmbedFieldBuilder> fields)
    {
        // Embedがサイズ制限を超える場合、以下ステップで段階的に削減
        if (ValidateEmbed(baseEmbed.WithFields(fields)))
        {
            return fields;
        }

        // フィールド数が最大25を超過している場合、先頭25件にトリム
        if (fields.Count > 25)
        {
            fields = [.. fields.Take(25)];
            Console.WriteLine("Fields truncated to 25 due to limit");
        }
        Console.WriteLine($"Fields Count: {fields.Count}");

        // 各フィールドを後ろからひとつずつ削除しつつ検証
        if (ValidateEmbed(baseEmbed.WithFields(fields)))
        {
            return fields;
        }

        if (fields.Count > 1)
        {
            for (var i = fields.Count - 1; i >= 0; i--)
            {
                Console.WriteLine($"Reducing field {i + 1}/{fields.Count}");
                EmbedFieldBuilder removedField = fields[i];
                fields.RemoveAt(i);
                if (ValidateEmbed(baseEmbed.WithFields(fields)))
                {
                    fields.Insert(i, removedField);
                    break;
                }
            }
        }

        // 最後のフィールドのテキストを行単位で削減して調整
        var lastFieldValue = fields.Last().Value.ToString();
        if (string.IsNullOrEmpty(lastFieldValue))
        {
            return fields;
        }
        var lastFieldValueLines = lastFieldValue.Split('\n');
        for (var i = lastFieldValueLines.Length - 1; i >= 0; i--)
        {
            Console.WriteLine($"Reducing field value {i + 1}/{lastFieldValueLines.Length} ({fields.Last().Value.ToString()?.Length})");
            lastFieldValueLines = lastFieldValueLines[..^1];
            fields.Last().Value = string.Join("\n", lastFieldValueLines);
            if (!ValidateEmbed(baseEmbed.WithFields(fields)))
            {
                continue;
            }
            fields.Last().Value += "\n...";
            break;
        }

        if (string.IsNullOrEmpty(fields.Last().Value.ToString()))
        {
            fields.RemoveAt(fields.Count - 1);
        }

        if (!ValidateEmbed(baseEmbed.WithFields(fields)))
        {
            throw new Exception("Embed is too long after reducing fields.");
        }

        return fields;
    }

    /// <summary>
    /// メンバー情報から EmbedFieldBuilder リストを生成する
    /// </summary>
    /// <param name="title">フィールドタイトル</param>
    /// <param name="members">メンバーリスト</param>
    /// <param name="memberTextFormat">メンバー表示形式</param>
    /// <returns>生成済み EmbedFieldBuilder リスト。メンバー無しは空リスト</returns>
    private List<EmbedFieldBuilder> GetMemberFields(string title, List<InstanceMember> members, MemberTextFormat memberTextFormat)
    {
        var membersString = GetMembersString(members, memberTextFormat);
        var splitMembers = membersString.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        Console.WriteLine($"Split members lines: {splitMembers.Count}");
        if (splitMembers.Count == 0)
        {
            return [];
        }

        // DiscordのEmbed制限内に収まるよう、行ごとに分割してフィールド化
        List<List<string>> slices = splitMembers.Aggregate(new List<List<string>> { new() }, (slices, member) =>
        {
            List<string> currentSlice = slices.Last();
            if (string.Join("\n", currentSlice.Append(member)).Length <= EmbedFieldBuilder.MaxFieldValueLength)
            {
                currentSlice.Add(member);
            }
            else
            {
                slices.Add([member]);
            }
            return slices;
        });

        var sliceMembers = slices.Select(slice => new EmbedFieldBuilder
        {
            Name = slices.IndexOf(slice) == 0 ? title : "\u200B",
            Value = string.Join("\n", slice),
            IsInline = false
        }).ToList();

        Console.WriteLine($"Slice Members Count: {sliceMembers.Count}");
        foreach (EmbedFieldBuilder? slice in sliceMembers)
        {
            Console.WriteLine($"Slice Members: {slice.Name} - {slice.Value.ToString()?.Length}");
        }

        return sliceMembers;
    }

    /// <summary>
    /// メンバーリストを行区切りの文字列に整形する
    /// </summary>
    /// <param name="members">メンバーリスト</param>
    /// <param name="memberTextFormat">メンバー表示形式</param>
    /// <returns>改行区切り整形文字列</returns>
    private string GetMembersString(List<InstanceMember> members, MemberTextFormat memberTextFormat)
    {
        // ユーザー名をバッククオートで囲み、オプションでリンクや参加時刻を追記
        var result = string.Join("\n", members.ConvertAll(member =>
        {
            var emoji = GetMemberEmoji(member);
            var escapedName = $"`{member.DisplayName}`";

            var text = memberTextFormat switch
            {
                MemberTextFormat.Full => $"{emoji} [{escapedName}](https://vrchat.com/home/user/{member.UserId}): {FormatDateTime(member.LastJoinAt)}" +
                    (member.LastLeaveAt.HasValue && member.LastLeaveAt > member.LastJoinAt ? $" - {FormatDateTime(member.LastLeaveAt)}" : ""),
                MemberTextFormat.ExcludeLinks => $"{emoji} {escapedName}: {FormatDateTime(member.LastJoinAt)}" +
                    (member.LastLeaveAt.HasValue && member.LastLeaveAt > member.LastJoinAt ? $" - {FormatDateTime(member.LastLeaveAt)}" : ""),
                MemberTextFormat.NameOnly => $"{emoji} {escapedName}",
                _ => throw new ArgumentOutOfRangeException(nameof(memberTextFormat), memberTextFormat, null)
            };
            return text;
        }));

        Console.WriteLine($"MembersString length: {result.Length}");
        return result;
    }

    /// <summary>
    /// メンバー状態に応じた絵文字を返す
    /// </summary>
    /// <param name="member">対象メンバー</param>
    /// <returns>オーナーは👑、自身は👤、フレンドは⭐️、その他は⬜️</returns>
    private string GetMemberEmoji(InstanceMember member)
    {
        if (member.IsInstanceOwner)
        {
            return "👑";
        }

        if (member.UserId == myLocation.UserId)
        {
            return "👤";
        }

        if (member.IsFriend)
        {
            return "⭐️";
        }

        return "⬜️";
    }

    /// <summary>
    /// メンバー表示形式を定義する列挙体
    /// </summary>
    private enum MemberTextFormat
    {
        /// <summary>
        /// フルフォーマット
        /// </summary>
        /// <example>{絵文字} [ユーザー名](https://vrchat.com/home/user/{ユーザーID}): {参加時刻} - {退出時刻}</example>
        Full,

        /// <summary>
        /// リンク省略
        /// </summary>
        /// <example>{絵文字} ユーザー名: {参加時刻} - {退出時刻}</example>
        ExcludeLinks,

        /// <summary>
        /// 名前のみ
        /// </summary>
        /// <example>{絵文字} ユーザー名</example>
        NameOnly,
    }
}