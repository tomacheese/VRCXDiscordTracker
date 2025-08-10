namespace VRCXDiscordTracker.Core.VRChat;

/// <summary>
/// インスタンスの情報を格納するクラス
/// </summary>
internal class VRChatInstance
{
    /// <summary>
    /// ワールドID
    /// </summary>
    /// <example>wrld_12345678-1234-1234-1234-123456789abc</example>
    public required string WorldId { get; set; }

    /// <summary>
    /// インスタンス名。通常は5桁の数字だが、任意の英数字文字列にすることも可能
    /// </summary>
    /// <example>12345</example>
    public required string InstanceName { get; set; }

    /// <summary>
    /// インスタンスタイプ
    /// </summary>
    /// <example>InstanceType.Friends</example>
    public required InstanceType Type { get; set; }

    /// <summary>
    /// インスタンスの所有者ID。ユーザーIDまたはグループID
    /// </summary>
    /// <example>usr_12345678-1234-1234-1234-123456789abc</example>
    public string? OwnerId { get; set; }

    /// <summary>
    /// インスタンスの地域
    /// </summary>
    /// <example>InstanceRegion.USWest</example>
    public required InstanceRegion Region { get; set; }

    /// <summary>
    /// ナンス
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789abc</example>
    public string? Nonce { get; set; }
}
