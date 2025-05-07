using System.Reflection;

namespace VRCXDiscordTracker.Core;
internal class AppConstants
{
    /// <summary>
    /// アプリケーション名
    /// </summary>
    public static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

    /// <summary>
    /// VRCXのデフォルトのSQLiteデータベースのパス
    /// </summary>
    public static readonly string VRCXDefaultDatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRCX", "VRCX.sqlite3");
}