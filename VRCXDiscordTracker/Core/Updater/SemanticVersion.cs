using System.Globalization;

namespace VRCXDiscordTracker.Core.Updater;

/// <summary>
/// セマンティックバージョンを表すクラス
/// </summary>
/// <param name="major">メジャーバージョン</param>
/// <param name="minor">マイナーバージョン</param>
/// <param name="patch">パッチバージョン</param>
internal class SemanticVersion(int major, int minor, int patch) : IComparable<SemanticVersion>
{
    /// <summary>
    /// メジャーバージョン
    /// </summary>
    public int Major { get; } = major;

    /// <summary>
    /// マイナーバージョン
    /// </summary>
    public int Minor { get; } = minor;

    /// <summary>
    /// パッチバージョン
    /// </summary>
    public int Patch { get; } = patch;

    /// <summary>
    /// セマンティックバージョンを文字列からパースする
    /// </summary>
    /// <param name="s">文字列</param>
    /// <returns>セマンティックバージョン</returns>
    /// <exception cref="FormatException">パースに失敗した場合</exception>
    public static SemanticVersion Parse(string s)
    {
        var parts = s.Split('.');
        if (parts.Length < 3)
            throw new FormatException("Invalid semantic version");
        return new SemanticVersion(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture),
            int.Parse(parts[2], CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// セマンティックバージョンを比較する
    /// </summary>
    /// <param name="other">比較対象</param>
    /// <returns>比較結果 (0:等しい, <0:小さい, >0:大きい)</returns>
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;
        if (Major != other.Major) return Major.CompareTo(other.Major);
        if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
        return Patch.CompareTo(other.Patch);
    }

    /// <summary>
    /// セマンティックバージョンを比較する演算子
    /// </summary>
    /// <param name="a">比較対象1</param>
    /// <param name="b">比較対象2</param>
    /// <returns>比較結果 (true: a > b)</returns>
    public static bool operator >(SemanticVersion a, SemanticVersion b)
        => a.CompareTo(b) > 0;

    /// <summary>
    /// セマンティックバージョンを比較する演算子
    /// </summary>
    /// <param name="a">比較対象1</param>
    /// <param name="b">比較対象2</param>
    /// <returns>比較結果 (true: a < b)</returns>
    public static bool operator <(SemanticVersion a, SemanticVersion b)
        => a.CompareTo(b) < 0;

    /// <summary>
    /// セマンティックバージョンの文字列表現
    /// </summary>
    /// <returns>文字列表現 ({メジャー}.{マイナー}.{パッチ})</returns>
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}