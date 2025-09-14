using System.Diagnostics;
using System.IO.Compression;

namespace VRCXDiscordTracker.Updater.Core;

/// <summary>
/// UpdaterHelper
/// </summary>
internal static class UpdaterHelper
{
    /// <summary>
    /// 指定したプロセス名のプロセスを全て終了させる。まずは CloseMainWindow() を呼び、5秒待ってから Kill() を呼ぶ。
    /// </summary>
    /// <param name="processName">プロセス名</param>
    public static void KillProcesses(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        foreach (Process proc in processes)
        {
            try
            {
                Console.WriteLine($"Requesting close for process Id={proc.Id}...");
                if (proc.CloseMainWindow())
                {
                    if (proc.WaitForExit(5000))
                    {
                        Console.WriteLine($"Process Id={proc.Id} exited gracefully.");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Process Id={proc.Id} did not exit within 5s, killing...");
                    }
                }
                else
                {
                    Console.WriteLine($"Process Id={proc.Id} has no main window or refused close, killing...");
                }

                proc.Kill();
                proc.WaitForExit();
                Console.WriteLine($"Process Id={proc.Id} killed.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to stop process Id={proc.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 指定したパスの ZIP ファイルを展開する
    /// </summary>
    /// <param name="zipPath">ZIP ファイルのパス</param>
    /// <param name="targetFolder">展開先フォルダ</param>
    public static void ExtractZipToTarget(string zipPath, string targetFolder)
    {
        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            // ディレクトリは飛ばす
            if (string.IsNullOrEmpty(entry.Name)) continue;

            // サニタイズされたパスを作成
            var dest = Path.Combine(targetFolder, entry.FullName);
            var fullPath = Path.GetFullPath(dest);

            // 展開先フォルダの外に出ないようにチェック
            if (!fullPath.StartsWith(Path.GetFullPath(targetFolder), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The ZIP file contains an incorrect path.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            entry.ExtractToFile(fullPath, overwrite: true);
        }
    }
}
