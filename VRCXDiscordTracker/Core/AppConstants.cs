using System.Reflection;

namespace VRCXDiscordTracker.Core;

/// <summary>
/// アプリケーションの定数を定義するクラス
/// </summary>
internal class AppConstants
{
    /// <summary>
    /// アプリケーション名
    /// </summary>
    public static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

    /// <summary>
    /// アプリケーションバージョンの文字列
    /// </summary>
    public static readonly string AppVersionString = (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0)).ToString(3); // Major.Minor.Patch

    /// <summary>
    /// VRCXのデフォルトのSQLiteデータベースのパス
    /// </summary>
    public static readonly string VRCXDefaultDatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRCX", "VRCX.sqlite3");

    /// <summary>
    /// GitHub リポジトリのオーナー名
    /// </summary>
    public const string GitHubRepoOwner = "tomacheese";

    /// <summary>
    /// GitHub リポジトリ名
    /// </summary>
    public const string GitHubRepoName = "VRCXDiscordTracker";
}
