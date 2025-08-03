using System.Diagnostics;
using System.Reflection;
using VRCXDiscordTracker.Updater.Core;

namespace VRCXDiscordTracker.Updater;

/// <summary>
/// アプリケーションアップデータ
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($"Application Updater {AppConstants.AppVersionString}");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        // 引数のパース
        // --app-name="<アプリ名>"
        // --target="<インストールフォルダ>"
        // --asset-name="<ダウンロードアセット名>"
        var appName = GetArgValue(args, "--app-name") ?? string.Empty;
        var target = GetArgValue(args, "--target") ?? string.Empty;
        var assetName = GetArgValue(args, "--asset-name") ?? string.Empty;
        var repoOwner = GetArgValue(args, "--repo-owner") ?? string.Empty;
        var repoName = GetArgValue(args, "--repo-name") ?? string.Empty;

        try
        {

            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(repoOwner) || string.IsNullOrEmpty(repoName))
            {
                throw new ArgumentException("Invalid arguments. Required: --app-name=<AppName> --target=<TargetFolder> --asset-name=<AssetName> --repo-owner=<RepoOwner> --repo-name=<RepoName>");
            }

            // 実行中Updaterがテンポラリフォルダにコピーされているか確認
            // コピーされていない場合は、コピーして再起動
            var tempRoot = Path.Combine(Path.GetTempPath(), appName, "Updater");
            var currentExe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExe))
            {
                throw new Exception("Failed to get current executable path.");
            }
            Console.WriteLine($"Current executable: {currentExe}");

            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyFileVersionAttribute>()!
                .Version;
            Console.WriteLine($"Version: {version}");

            var versionFolder = Path.Combine(tempRoot, version);
            var selfCopyExe = Path.Combine(versionFolder, Path.GetFileName(currentExe));

            if (!currentExe.Equals(selfCopyExe, StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
                Directory.CreateDirectory(versionFolder);
                Console.WriteLine($"{currentExe} -> {selfCopyExe}");
                File.Copy(currentExe, selfCopyExe, overwrite: true);

                Console.WriteLine("Restarting updater...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = selfCopyExe,
                    UseShellExecute = false,
                    ArgumentList = {
                        $"--app-name={appName}",
                        $"--target={target}",
                        $"--asset-name={assetName}",
                        $"--repo-owner={repoOwner}",
                        $"--repo-name={repoName}"
                    },
                });
                return;
            }

            // 最新リリースの取得
            var gh = new GitHubReleaseService(repoOwner, repoName);
            ReleaseInfo latest = await gh.GetLatestReleaseAsync(assetName).ConfigureAwait(false);

            // ダウンロード
            Console.WriteLine($"Downloading v{latest.Version} ...");
            var userAgent = $"{repoOwner} {repoName} ({AppConstants.AppVersionString})";
            var zipPath = await gh.DownloadWithProgressAsync(latest.AssetUrl).ConfigureAwait(false);

            // アプリ停止
            Console.WriteLine("Stopping running processes...");
            UpdaterHelper.KillProcesses(appName);

            // Zipファイル展開
            Console.WriteLine("Extracting to target folder...");
            UpdaterHelper.ExtractZipToTarget(zipPath, target);

            // アプリ再起動
            Console.WriteLine("Launching application...");
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(target, appName + ".exe"),
                UseShellExecute = true,
                WorkingDirectory = target,
            });

            // Zipファイル削除
            Console.WriteLine("Cleaning up...");
            File.Delete(zipPath);

            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(ex.StackTrace).ConfigureAwait(false);
            await Console.Error.WriteLineAsync("Press any key to start in update skip mode.").ConfigureAwait(false);
            Console.ReadKey(true);

            // アプリケーションをアップデートスキップモードで起動
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(target, appName + ".exe"),
                Arguments = "--skip-update",
                UseShellExecute = true,
                WorkingDirectory = target,
            });
        }

    }

    private static string? GetArgValue(string[] args, string key)
    {
        return args
            .Where(arg => arg.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            .Select(arg =>
            {
                var value = arg[key.Length..];
                if (value.StartsWith('=') || value.StartsWith(':') || value.StartsWith(' '))
                    value = value[1..];
                return value.Trim('"', '\'');
            })
            .FirstOrDefault();
    }
}
