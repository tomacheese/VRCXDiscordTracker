using System.Reflection;

namespace VRCXDiscordTracker.Core.VRChat;

/// <summary>
/// VRChatのインスタンスの種類を表すクラス
/// </summary>
/// <param name="id">管理用ID</param>
/// <param name="name">表示名</param>
/// <param name="isGroup">グループに関連するインスタンスかどうか</param>"
internal class InstanceType(int id, string name, bool isGroup) : IComparable<InstanceType>, IEquatable<InstanceType>, IFormattable
{
    /// <summary>
    /// Public
    /// </summary>
    public static readonly InstanceType Public = new(0, "Public", false);

    /// <summary>
    /// Friends+
    /// </summary>
    public static readonly InstanceType FriendsPlus = new(1, "Friends+", false);

    /// <summary>
    /// Friends
    /// </summary>
    public static readonly InstanceType Friends = new(2, "Friends", false);

    /// <summary>
    /// Invite+
    /// </summary>
    public static readonly InstanceType InvitePlus = new(3, "Invite+", false);

    /// <summary>
    /// Invite
    /// </summary>
    public static readonly InstanceType Invite = new(4, "Invite", false);

    /// <summary>
    /// Group Public
    /// </summary>
    public static readonly InstanceType GroupPublic = new(5, "Group Public", true);

    /// <summary>
    /// Group+
    /// </summary>
    public static readonly InstanceType GroupPlus = new(6, "Group+", true);

    /// <summary>
    /// Group
    /// </summary>
    public static readonly InstanceType Group = new(7, "Group", true);

    /// <summary>
    /// 管理用ID
    /// </summary>
    public readonly int Id = id;

    /// <summary>
    /// 表示名
    /// </summary>
    /// <example>Public</example>
    public readonly string Name = name;

    /// <summary>
    /// グループに関連するインスタンスかどうか
    /// </summary>
    /// <example>true (Group Public, Group+, Group)</example>
    public readonly bool IsGroup = isGroup;

    public static IEnumerable<T> GetAll<T>() where T : class
    {
        return typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.FieldType == typeof(T))
            .Select(field => field.GetValue(null) as T)
            .Where(instance => instance != null)!;
    }

    public int CompareTo(InstanceType? other) => other == null ? 1 : Id == other.Id ? 0 : Id.CompareTo(other.Id);

    public override bool Equals(object? obj) => obj is InstanceType other && Equals(other);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.IsNullOrEmpty(format)
            ? Name
            : format.Equals("id", StringComparison.OrdinalIgnoreCase)
            ? Id.ToString(formatProvider)
            : format.Equals("name", StringComparison.OrdinalIgnoreCase)
            ? Name
            : throw new FormatException($"The format '{format}' is not supported.");
    }

    public override int GetHashCode() => Id.GetHashCode();

    public bool Equals(InstanceType? other) => other != null && Id == other.Id;
}
