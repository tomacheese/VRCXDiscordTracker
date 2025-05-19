using System.Diagnostics;

namespace VRCXDiscordTracker.Core.Updater;

/// <summary>
/// アップデートを確認するクラス
/// </summary>
/// <param name="gh">GitHubReleaseService</param>
internal class UpdateChecker(GitHubReleaseService gh)
{
    /// <summary>
    /// 最新リリース情報
    /// </summary>
    private ReleaseInfo? _latest = null;

    /// <summary>
    /// 最新リリース情報を取得する
    /// </summary>
    /// <returns>最新リリース情報</returns>
    public async Task<ReleaseInfo> GetLatestRelease()
    {
        _latest = await gh.GetLatestReleaseAsync("VRCXDiscordTracker.zip");
        return _latest;
    }

    /// <summary>
    /// アップデートが可能かどうかを確認する
    /// </summary>
    /// <returns>アップデート可能な場合はtrue</returns>
    /// <exception cref="InvalidOperationException">GetLatestReleaseAsyncが呼ばれていない場合</exception>
    public bool IsUpdateAvailable()
    {
        if (_latest == null)
        {
            throw new InvalidOperationException("GetLatestReleaseAsync must be called before IsUpdateAvailable.");
        }

        var localVersion = SemanticVersion.Parse(AppConstants.AppVersionString);
        return _latest.Version > localVersion;
    }

    /// <summary>
    /// アップデートを確認する
    /// </summary>
    /// <returns>アップデート処理の開始に成功した場合はtrue</returns>
    /// <exception cref="FileNotFoundException">アップデータが見つからない場合</exception>
    public static async Task<bool> Check()
    {
        try
        {
            var gh = new GitHubReleaseService(AppConstants.GitHubRepoOwner, AppConstants.GitHubRepoName);
            var checker = new UpdateChecker(gh);
            ReleaseInfo latest = await checker.GetLatestRelease();
            if (!checker.IsUpdateAvailable())
            {
                Console.WriteLine("No update available.");
                return false;
            }

            Console.WriteLine($"Update available ({AppConstants.AppVersionString} -> {latest}). Updating...");
            var updaterPath = Path.Combine(Application.StartupPath, "VRCXDiscordTracker.Updater.exe");
            if (!File.Exists(updaterPath))
            {
                throw new FileNotFoundException("Updater executable not found.", updaterPath);
            }

            var processPath = Environment.ProcessPath;
            var appName = Path.GetFileNameWithoutExtension(processPath);
            var target = Path.GetDirectoryName(processPath);
            var repoOwner = AppConstants.GitHubRepoOwner;
            var repoName = AppConstants.GitHubRepoName;

            var assetName = "VRCXDiscordTracker.zip";
            if (string.IsNullOrEmpty(target))
            {
                throw new Exception("Failed to get target directory.");
            }
            Console.WriteLine("Starting updater...");
            Process.Start(new ProcessStartInfo
            {
                FileName = updaterPath,
                ArgumentList = {
                    $"--app-name={appName}",
                    $"--target={target}",
                    $"--asset-name={assetName}",
                    $"--repo-owner={repoOwner}",
                    $"--repo-name={repoName}"
                },
                UseShellExecute = false
            });
            Application.Exit();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Update check failed: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return false;
        }
    }
}