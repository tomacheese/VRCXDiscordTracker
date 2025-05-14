using System.Reflection;

namespace VRCXDiscordTracker.Core;
internal class AppConstants
{
    /// <summary>
    /// アプリケーション名
    /// </summary>
    public static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

    /// <summary>
    /// アプリケーションバージョン
    /// </summary>
    public static readonly Version AppVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    /// <summary>
    /// VRCXのデフォルトのSQLiteデータベースのパス
    /// </summary>
    public static readonly string VRCXDefaultDatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRCX", "VRCX.sqlite3");
}