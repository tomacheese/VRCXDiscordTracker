using System.Reflection;

namespace VRCXDiscordTracker.Updater.Core;
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
    /// アプリケーションバージョンの文字列
    /// </summary>
    public static readonly string AppVersionString = AppVersion.ToString(3); // Major.Minor.Patch
}