namespace VRCXDiscordTracker.Core;
internal class AppConstants
{
    /// <summary>
    /// VRCXのデフォルトのSQLiteデータベースのパス
    /// </summary>
    public static readonly string VRCXDefaultDatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRCX", "VRCX.sqlite3");
}
