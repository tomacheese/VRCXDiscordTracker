using System.Reflection;

namespace VRCXDiscordTracker.Core.VRChat;

/// <summary>
/// VRChatのインスタンスの種類を表すクラス
/// </summary>
/// <param name="id">管理用ID</param>
/// <param name="name">表示名</param>
internal class InstanceType(int id, string name) : IComparable<InstanceType>, IEquatable<InstanceType>, IFormattable
{
    /// <summary>
    /// Public
    /// </summary>
    public static readonly InstanceType Public = new(0, "Public");

    /// <summary>
    /// Friends+
    /// </summary>
    public static readonly InstanceType FriendsPlus = new(1, "Friends+");

    /// <summary>
    /// Friends
    /// </summary>
    public static readonly InstanceType Friends = new(2, "Friends");

    /// <summary>
    /// Invite+
    /// </summary>
    public static readonly InstanceType InvitePlus = new(3, "Invite+");

    /// <summary>
    /// Invite
    /// </summary>
    public static readonly InstanceType Invite = new(4, "Invite");

    /// <summary>
    /// Group Public
    /// </summary>
    public static readonly InstanceType GroupPublic = new(5, "Group Public");

    /// <summary>
    /// Group+
    /// </summary>
    public static readonly InstanceType GroupPlus = new(6, "Group+");

    /// <summary>
    /// Group
    /// </summary>
    public static readonly InstanceType Group = new(7, "Group");

    /// <summary>
    /// 管理用ID
    /// </summary>
    public readonly int Id = id;

    /// <summary>
    /// 表示名
    /// </summary>
    /// <example>Public</example>
    public readonly string Name = name;

    public static IEnumerable<T> GetAll<T>() where T : class
    {
        return typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.FieldType == typeof(T))
            .Select(field => field.GetValue(null) as T)
            .Where(instance => instance != null)!;
    }

    public int CompareTo(InstanceType? other)
    {
        if (other == null) return 1;
        if (Id == other.Id) return 0;
        return Id.CompareTo(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is InstanceType other)
            return Equals(other);

        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format))
            return Name;
        if (format.Equals("id", StringComparison.OrdinalIgnoreCase))
            return Id.ToString(formatProvider);
        if (format.Equals("name", StringComparison.OrdinalIgnoreCase))
            return Name;

        throw new FormatException($"The format '{format}' is not supported.");
    }

    public override int GetHashCode() => Id.GetHashCode();

    public bool Equals(InstanceType? other)
    {
        if (other == null) return false;
        if (Id == other.Id) return true;
        return false;
    }
}