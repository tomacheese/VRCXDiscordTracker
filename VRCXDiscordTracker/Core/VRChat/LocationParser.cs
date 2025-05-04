using System.Text.RegularExpressions;

namespace VRCXDiscordTracker.Core.VRChat;

/// <summary>
/// VRChatのロケーションIDを解析するクラス
/// </summary>
internal partial class LocationParser
{
    /// <summary>
    /// ロケーションIDの正規表現
    /// </summary>
    [GeneratedRegex(@"^(?<world>wrld_[0-9a-fA-F-]+):(?<instance>[A-z0-9_-]+)(?<tokens>(~[^~]+)*)$", RegexOptions.Compiled)]
    private static partial Regex LocationRegex();

    /// <summary>
    /// ユーザーIDの正規表現
    /// </summary>
    [GeneratedRegex(@"\((?<userId>usr_[0-9a-fA-F-]+)\)", RegexOptions.Compiled)]
    public static partial Regex UserRegex();

    /// <summary>
    /// ロケーションIDを解析して、VRChatInstanceオブジェクトを生成する
    /// </summary>
    /// <param name="locationId">ロケーションID</param>
    /// <returns>VRChatInstanceオブジェクト</returns>
    /// <exception cref="ArgumentException">ロケーションIDがnullまたは空文字列の場合</exception>
    /// <exception cref="FormatException">ロケーションIDの形式が不正な場合</exception>
    public static VRChatInstance Parse(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
            throw new ArgumentException("Location ID cannot be null or empty.", nameof(locationId));

        // ローカルインスタンス "local:" は対象外
        if (locationId.StartsWith("local:"))
            throw new FormatException("Local instances are not supported.");
        // オフラインインスタンス "offline:" は対象外
        if (locationId.StartsWith("offline:"))
            throw new FormatException("Offline instances are not supported.");
        // トラベリングインスタンス "traveling:" は対象外
        if (locationId.StartsWith("traveling:"))
            throw new FormatException("Traveling instances are not supported.");

        Match m = LocationRegex().Match(locationId);
        if (!m.Success)
            throw new FormatException("Invalid location ID format.");

        var world = m.Groups["world"].Value;
        var instanceNumber = m.Groups["instance"].Value;
        var tokens = m.Groups["tokens"].Value.Split('~', StringSplitOptions.RemoveEmptyEntries);

        var extractedTokens = ExtractedTokens.Parse(tokens);

        var inst = new VRChatInstance
        {
            WorldId = world,
            InstanceName = instanceNumber,
            Type = GetInstanceType(extractedTokens),
            OwnerId = extractedTokens.CreatorId ?? extractedTokens.GroupId,
            Region = extractedTokens.Region ?? InstanceRegion.USWest,
            Nonce = extractedTokens.Nonce
        };

        return inst;
    }

    /// <summary>
    /// 抽出トークンからインスタンスタイプを取得する
    /// </summary>
    /// <param name="extractedTokens">抽出トークン</param>
    /// <returns>インスタンスタイプ</returns>
    private static InstanceType GetInstanceType(ExtractedTokens extractedTokens)
    {
        // デフォルトは Public
        InstanceType type = InstanceType.Public;

        // フレンド系
        if (extractedTokens.IsHiddenToken)
        {
            type = InstanceType.FriendsPlus;
        }
        else if (extractedTokens.IsFriendsToken)
        {
            type = InstanceType.Friends;
        }

        // 招待系
        else if (extractedTokens.IsInviteToken)
        {
            type = extractedTokens.CanRequestInvite
                ? InstanceType.InvitePlus
                : InstanceType.Invite;
        }

        // グループ系
        if (extractedTokens.IsGroupToken)
        {
            // groupAccessType(xxx) から中身を抽出
            var access = extractedTokens.GroupAccessType;

            type = access switch
            {
                "members" => InstanceType.Group,
                "plus" => InstanceType.GroupPlus,
                "public" => InstanceType.GroupPublic,
                _ => InstanceType.Group
            };
        }

        return type;
    }

    /// <summary>
    /// ロケーションIDから抽出したトークンを解析し、情報種別ごとに保持するクラス
    /// </summary>
    private class ExtractedTokens
    {
        public InstanceRegion? Region { get; set; }
        public string? GroupId { get; set; }
        public string? GroupAccessType { get; set; }
        public bool CanRequestInvite { get; set; }
        public string? CreatorId { get; set; }
        public required bool IsHiddenToken { get; set; }
        public required bool IsFriendsToken { get; set; }
        public required bool IsInviteToken { get; set; }
        public required bool IsGroupToken { get; set; }
        public string? Nonce { get; set; }

        /// <summary>
        /// トークンを解析して、ExtractedTokensオブジェクトを生成する
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public static ExtractedTokens Parse(string[] tokens)
        {
            var regionToken = tokens.FirstOrDefault(t => t.StartsWith("region("))?[7..^1];
            var creatorIdToken = tokens.FirstOrDefault(t => UserRegex().IsMatch(t));
            Match creatorIdMatch = UserRegex().Match(creatorIdToken ?? string.Empty);
            var creatorId = creatorIdMatch.Success ? creatorIdMatch.Groups["userId"].Value : null;
            return new ExtractedTokens
            {
                // Region: region(jp), region(eu), region(use), region(us)
                Region = InstanceRegion.GetByToken(regionToken),
                // Group: group(grp_12345)
                GroupId = tokens.FirstOrDefault(t => t.StartsWith("group("))?[6..^1],
                // Group Access Type: groupAccessType(members), groupAccessType(plus), groupAccessType(public)
                GroupAccessType = tokens.FirstOrDefault(t => t.StartsWith("groupAccessType("))?[16..^1],
                // Can Request Invite: canRequestInvite
                CanRequestInvite = tokens.Contains("canRequestInvite"),
                // CreatorId: (usr_0b83d9be-9852-42dd-98e2-625062400acc)
                CreatorId = creatorId,
                // hidden(usr_0b83d9be-9852-42dd-98e2-625062400acc)
                IsHiddenToken = tokens.FirstOrDefault(t => t.StartsWith("hidden(")) != null,
                // friends(usr_0b83d9be-9852-42dd-98e2-625062400acc)
                IsFriendsToken = tokens.FirstOrDefault(t => t.StartsWith("friends(")) != null,
                // private(usr_0b83d9be-9852-42dd-98e2-625062400acc)
                IsInviteToken = tokens.FirstOrDefault(t => t.StartsWith("private(")) != null,
                // group(grp_12345)
                IsGroupToken = tokens.FirstOrDefault(t => t.StartsWith("group(")) != null,
                // Nonce: nonce(6ba04d44-1774-4c70-a92d-4438615d6962)
                Nonce = tokens.FirstOrDefault(t => t.StartsWith("nonce("))?[6..^1],
            };
        }
    }
}