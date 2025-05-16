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

        EmbedFieldPattern[] patterns = [
            // フィールドパターン1: フルフォーマットのCurrent+Past
            new EmbedFieldPattern { CurrentFormat = MemberTextFormat.Full, PastFormat = MemberTextFormat.Full, IsReducible = false },
            // フィールドパターン2: Currentはフル、Pastはリンク省略
            new EmbedFieldPattern { CurrentFormat = MemberTextFormat.Full, PastFormat = MemberTextFormat.ExcludeLinks, IsReducible = false },
            // フィールドパターン3: Currentはフル、Pastは最小限
            new EmbedFieldPattern { CurrentFormat = MemberTextFormat.Full, PastFormat = MemberTextFormat.NameOnly, IsReducible = false },
            // フィールドパターン4: Currentはリンク省略、Pastは最小限
            new EmbedFieldPattern { CurrentFormat = MemberTextFormat.ExcludeLinks, PastFormat = MemberTextFormat.NameOnly, IsReducible = false },
            // フィールドパターン5: 両方とも最小限
            new EmbedFieldPattern { CurrentFormat = MemberTextFormat.NameOnly, PastFormat = MemberTextFormat.NameOnly, IsReducible = false },
            // 最終パターン: 最小限フィールドをReduceFieldsでさらに文字数調整
            new EmbedFieldPattern { CurrentFormat = MemberTextFormat.NameOnly, PastFormat = MemberTextFormat.NameOnly, IsReducible = true }
        ];


        List<InstanceMember> currentMembers = instanceMembers.FindAll(member => member.IsCurrently);
        List<InstanceMember> pastMembers = instanceMembers.FindAll(member => !member.IsCurrently);
        Console.WriteLine($"CurrentMembers count: {currentMembers.Count}");
        Console.WriteLine($"PastMembers count: {pastMembers.Count}");

        foreach (EmbedFieldPattern pattern in patterns)
        {
            Console.WriteLine($"Trying pattern with Current: {pattern.CurrentFormat}, Past: {pattern.PastFormat}");
            List<EmbedFieldBuilder> currentFields = GetMemberFields(MemberStatus.Currently, currentMembers, pattern.CurrentFormat);
            List<EmbedFieldBuilder> pastFields = GetMemberFields(MemberStatus.Past, pastMembers, pattern.PastFormat);
            var combinedFields = currentFields.Concat(pastFields).ToList();
            if (pattern.IsReducible)
            {
                combinedFields = ReduceFields(baseEmbed, combinedFields);
            }

            EmbedBuilder patternEmbed = SetFields(baseEmbed, combinedFields);
            if (ValidateEmbed(patternEmbed))
            {
                Console.WriteLine($"Selected build pattern with Current: {pattern.CurrentFormat}, Past: {pattern.PastFormat}");
                return patternEmbed.Build();
            }
        }

        // 基本的には発生しない
        throw new Exception("Failed to build a valid embed with the given patterns.");
    }

    /// <summary>
    /// タイトル、説明、著者、フッター、色などの基本設定を持つ EmbedBuilder を生成する
    /// </summary>
    /// <returns>基本設定済み EmbedBuilder</returns>
    /// <exception cref="FormatException">ロケーション ID の形式不正</exception>
    internal EmbedBuilder GetBaseEmbed()
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
    internal static bool ValidateEmbed(EmbedBuilder embed)
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
    internal static EmbedBuilder SetFields(EmbedBuilder embed, List<EmbedFieldBuilder> fields)
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
    internal static string Sanitize(string text)
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
    internal static string FormatDateTime(DateTime? dateTime) => dateTime?.ToString("G", CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>
    /// Embed のサイズ制限内に収まるようフィールド数・文字数を削減する
    /// </summary>
    /// <param name="baseEmbed">基本 EmbedBuilder</param>
    /// <param name="fields">初期 EmbedFieldBuilder リスト</param>
    /// <returns>制限内調整済み EmbedFieldBuilder リスト</returns>
    /// <exception cref="Exception">サイズ制限超過時</exception>
    internal static List<EmbedFieldBuilder> ReduceFields(EmbedBuilder baseEmbed, List<EmbedFieldBuilder> fields)
    {
        if (ValidateEmbed(baseEmbed.WithFields(fields)))
        {
            return fields;
        }

        Console.WriteLine($"Fields Count: {fields.Count}");

        // 各フィールドを後ろからひとつずつ削除しつつ検証
        fields = fields.Take(25).ToList();
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
    /// <param name="memberStatus">メンバー状態</param>
    /// <param name="members">メンバーリスト</param>
    /// <param name="memberTextFormat">メンバー表示形式</param>
    /// <returns>生成済み EmbedFieldBuilder リスト。メンバー無しは空リスト</returns>
    internal List<EmbedFieldBuilder> GetMemberFields(MemberStatus memberStatus, List<InstanceMember> members, MemberTextFormat memberTextFormat)
    {
        var title = memberStatus switch
        {
            MemberStatus.Currently => "Current Members",
            MemberStatus.Past => "Past Members",
            _ => throw new ArgumentOutOfRangeException(nameof(memberStatus), memberStatus, null)
        };

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
            Value = string.Join("\n", slice).Length > EmbedFieldBuilder.MaxFieldValueLength
                ? string.Join("\n", slice).Substring(0, EmbedFieldBuilder.MaxFieldValueLength - 3) + "..."
                : string.Join("\n", slice),
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
    internal string GetMembersString(List<InstanceMember> members, MemberTextFormat memberTextFormat)
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
    internal string GetMemberEmoji(InstanceMember member)
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
    /// EmbedField のパターンを定義するレコード
    /// </summary>
    private record EmbedFieldPattern
    {
        /// <summary>
        /// Current Members の表示形式
        /// </summary>
        public required MemberTextFormat CurrentFormat { get; init; }

        /// <summary>
        /// Past Members の表示形式
        /// </summary>
        public required MemberTextFormat PastFormat { get; init; }

        /// <summary>
        /// フィールドが削減可能とするかどうか
        /// </summary>
        public required bool IsReducible { get; init; }
    }

    /// <summary>
    /// メンバーの状態を定義する列挙体
    /// </summary>
    internal enum MemberStatus
    {
        /// <summary>
        /// 現在のインスタンスに参加中
        /// </summary>

        Currently,
        /// <summary>
        /// 過去のインスタンスに参加していた
        /// </summary>
        Past,
    }

    /// <summary>
    /// メンバー表示形式を定義する列挙体
    /// </summary>
    internal enum MemberTextFormat
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