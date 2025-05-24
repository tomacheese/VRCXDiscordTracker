# Program.cs (Updater) レビュー

## 概要

`VRCXDiscordTracker.Updater`プロジェクトの`Program.cs`は、アプリケーションの自動更新処理を行うためのエントリーポイントです。GitHubリリースから最新バージョンの取得、ダウンロード、展開、およびアプリケーションの再起動を担当しています。

## 現状のコード

```csharp
namespace VRCXDiscordTracker.Updater;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using VRCXDiscordTracker.Updater.Core;

internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            DisplayHeader();
            var options = ParseCommandLineArguments(args);
            ValidateOptions(options);
            
            if (await SelfCopyIfNeeded(options))
                return; // 再起動した場合は終了
                
            await PerformUpdate(options);
            
            // 成功したらクリーンアップを実行
            CleanupTempFiles();
        }
        catch (Exception ex)
        {
            HandleUpdateError(ex, options);
        }
    }

    private static void DisplayHeader()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($"Application Updater {AppConstants.AppVersionString}");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
    }

    private static async Task PerformUpdate(UpdateOptions options)
    {
        // 1. GitHubからリリース情報を取得
        var releaseInfo = await GetLatestReleaseAsync(options);
        
        // 2. 更新対象フォルダのバックアップを作成
        var backupPath = CreateBackup(options.TargetFolder);
        
        try
        {
            // 3. 実行中のアプリを停止
            StopRunningApplication(options.AppName);
            
            // 4. 更新用ZIPをダウンロード
            var zipPath = await DownloadUpdatePackage(releaseInfo);
            
            // 5. ZIPファイルの整合性を検証
            if (!await VerifyFileIntegrity(zipPath, releaseInfo.ChecksumSha256))
            {
                throw new SecurityException("Downloaded file integrity check failed");
            }
            
            // 6. ZIPファイルを展開
            ExtractZipSecurely(zipPath, options.TargetFolder);
            
            // 7. アプリケーションを起動
            LaunchApplication(options.TargetFolder, options.AppName);
            
            // 8. 一時ファイルを削除
            CleanupDownloadedFiles(zipPath);
        }
        catch (Exception)
        {
            // 更新に失敗した場合はバックアップから復元
            RestoreFromBackup(backupPath, options.TargetFolder);
            throw;
        }
    }

    private static UpdateOptions ParseCommandLineArguments(string[] args)
    {
        return new UpdateOptions
        {
            AppName = GetArgValue(args, CommandLineArgs.AppName) ?? string.Empty,
            TargetFolder = GetArgValue(args, CommandLineArgs.Target) ?? string.Empty,
            AssetName = GetArgValue(args, CommandLineArgs.AssetName) ?? string.Empty,
            RepoOwner = GetArgValue(args, CommandLineArgs.RepoOwner) ?? string.Empty,
            RepoName = GetArgValue(args, CommandLineArgs.RepoName) ?? string.Empty,
            DevMode = args.Any(arg => arg.Equals(CommandLineArgs.DevMode, StringComparison.OrdinalIgnoreCase))
        };
    }

    private static void ValidateOptions(UpdateOptions options)
    {
        var errors = options.GetValidationErrors().ToArray();
        if (errors.Length > 0)
        {
            throw new ArgumentException(
                $"Invalid arguments:\n{string.Join("\n", errors)}\n\n" +
                $"Required: {CommandLineArgs.AppName}=<AppName> {CommandLineArgs.Target}=<TargetFolder> " +
                $"{CommandLineArgs.AssetName}=<AssetName> {CommandLineArgs.RepoOwner}=<RepoOwner> {CommandLineArgs.RepoName}=<RepoName>");
        }

        // ターゲットフォルダの書き込み権限を確認
        if (!CheckWritePermission(options.TargetFolder))
        {
            throw new UnauthorizedAccessException($"No write permission to target folder: {options.TargetFolder}");
        }
    }

    private static async Task<string> DownloadUpdatePackage(ReleaseInfo releaseInfo)
    {
        Console.WriteLine($"Downloading v{releaseInfo.Version} ...");
        
        // 明示的なHTTPクライアントの破棄
        using var httpClient = new HttpClient();
        var userAgent = $"{releaseInfo.RepoOwner} {releaseInfo.RepoName} ({AppConstants.AppVersionString})";
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        
        var zipPath = Path.GetTempFileName();
        try
        {
            using var response = await httpClient.GetAsync(releaseInfo.AssetUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalSize = response.Content.Headers.ContentLength ?? -1L;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            
            // 進捗表示付きでダウンロード
            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            
            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                downloaded += read;
                
                if (totalSize > 0)
                {
                    var percentage = (int)((downloaded * 100) / totalSize);
                    Console.Write($"\rDownloading: {downloaded:#,0}/{totalSize:#,0} bytes ({percentage}%)");
                }
                else
                {
                    Console.Write($"\rDownloading: {downloaded:#,0} bytes");
                }
            }
            Console.WriteLine();
            
            return zipPath;
        }
        catch
        {
            // エラー時のクリーンアップ
            if (File.Exists(zipPath))
            {
                try { File.Delete(zipPath); } catch { /* 削除に失敗しても続行 */ }
            }
            throw;
        }
    }

    private static void CleanupTempFiles()
    {
        // テンポラリディレクトリの古いバージョンフォルダを削除
        try
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), AppConstants.AppName, "Updater");
            if (Directory.Exists(tempRoot))
            {
                var currentVersion = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                    
                foreach (var versionDir in Directory.GetDirectories(tempRoot))
                {
                    var dirName = Path.GetFileName(versionDir);
                    if (dirName != currentVersion)
                    {
                        try
                        {
                            Directory.Delete(versionDir, recursive: true);
                            Console.WriteLine($"Cleaned up old updater version: {dirName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to cleanup old updater version {dirName}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to cleanup temporary files: {ex.Message}");
        }
    }
}
```

## レビュー内容

### 良い点

1. ✅ **自己更新機能**: アップデーター自体が一時フォルダにコピーされて実行される仕組みにより、アップデート中にアップデーター自体をロックから解放
2. ✅ **エラーハンドリング**: 例外発生時にユーザーフレンドリーな対応（アップデートスキップモードでのアプリ起動）
3. ✅ **引数のバリデーション**: 必須パラメータのチェックが適切に実装されている
4. ✅ **進捗表示**: ダウンロード進捗がコンソールに表示される
5. ✅ **アトミックな更新処理**: 更新処理が完了するまで元のアプリケーションを起動しないことで、不完全な更新状態を防止している

### 問題点

1. ⚠️ **メソッドの責務が大きい**: `Main`メソッドが多くの責務を持ち、非常に長い
2. ⚠️ **ハードコードされた文字列**: エラーメッセージやコマンドライン引数名がハードコードされている
3. ⚠️ **セキュリティリスク**: 例外時のスタックトレース表示によって内部情報が漏洩する可能性
4. ⚠️ **クリーンアップ処理の不足**: 一時ファイルが残る可能性がある（特に例外発生時）
5. ⚠️ **再利用可能なコードの分離不足**: 引数処理やプロセス起動などの汎用的な機能が分離されていない
6. ⚠️ **リソース管理**: `using` ステートメントの不使用によるリソースリークの可能性
7. ⚠️ **設定の外部化がされていない**: 環境変数やコンフィグレーションファイルからの設定読み込みがない

### リファクタリング提案

#### 1. 責務の分離

```csharp
static async Task Main(string[] args)
{
    try
    {
        DisplayHeader();
        var options = ParseCommandLineArguments(args);
        ValidateOptions(options);
        
        if (await SelfCopyIfNeeded(options))
            return; // 再起動した場合は終了
            
        await PerformUpdate(options);
        
        // 成功したらクリーンアップを実行
        CleanupTempFiles();
    }
    catch (Exception ex)
    {
        HandleUpdateError(ex, options);
    }
}

private static void DisplayHeader()
{
    Console.WriteLine("--------------------------------------------------");
    Console.WriteLine($"Application Updater {AppConstants.AppVersionString}");
    Console.WriteLine("--------------------------------------------------");
    Console.WriteLine();
}

private static async Task PerformUpdate(UpdateOptions options)
{
    // 1. GitHubからリリース情報を取得
    var releaseInfo = await GetLatestReleaseAsync(options);
    
    // 2. 更新対象フォルダのバックアップを作成
    var backupPath = CreateBackup(options.TargetFolder);
    
    try
    {
        // 3. 実行中のアプリを停止
        StopRunningApplication(options.AppName);
        
        // 4. 更新用ZIPをダウンロード
        var zipPath = await DownloadUpdatePackage(releaseInfo);
        
        // 5. ZIPファイルの整合性を検証
        if (!await VerifyFileIntegrity(zipPath, releaseInfo.ChecksumSha256))
        {
            throw new SecurityException("Downloaded file integrity check failed");
        }
        
        // 6. ZIPファイルを展開
        ExtractZipSecurely(zipPath, options.TargetFolder);
        
        // 7. アプリケーションを起動
        LaunchApplication(options.TargetFolder, options.AppName);
        
        // 8. 一時ファイルを削除
        CleanupDownloadedFiles(zipPath);
    }
    catch (Exception)
    {
        // 更新に失敗した場合はバックアップから復元
        RestoreFromBackup(backupPath, options.TargetFolder);
        throw;
    }
}
```

#### 2. コマンドライン引数の構造化とバリデーション強化

```csharp
// 引数を定数化して再利用性を高める
private static class CommandLineArgs
{
    public const string AppName = "--app-name";
    public const string Target = "--target";
    public const string AssetName = "--asset-name";
    public const string RepoOwner = "--repo-owner";
    public const string RepoName = "--repo-name";
    public const string DevMode = "--dev-mode";
}

private class UpdateOptions
{
    public string AppName { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string RepoOwner { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public bool DevMode { get; set; } = false;
    
    public bool IsValid => 
        !string.IsNullOrEmpty(AppName) && 
        !string.IsNullOrEmpty(TargetFolder) && 
        !string.IsNullOrEmpty(AssetName) &&
        !string.IsNullOrEmpty(RepoOwner) && 
        !string.IsNullOrEmpty(RepoName);
        
    public IEnumerable<string> GetValidationErrors()
    {
        if (string.IsNullOrEmpty(AppName))
            yield return $"Missing required argument: {CommandLineArgs.AppName}";
        if (string.IsNullOrEmpty(TargetFolder))
            yield return $"Missing required argument: {CommandLineArgs.Target}";
        if (string.IsNullOrEmpty(AssetName))
            yield return $"Missing required argument: {CommandLineArgs.AssetName}";
        if (string.IsNullOrEmpty(RepoOwner))
            yield return $"Missing required argument: {CommandLineArgs.RepoOwner}";
        if (string.IsNullOrEmpty(RepoName))
            yield return $"Missing required argument: {CommandLineArgs.RepoName}";
    }
    
    public string[] ToArgumentArray()
    {
        return new[]
        {
            $"{CommandLineArgs.AppName}={AppName}",
            $"{CommandLineArgs.Target}={TargetFolder}",
            $"{CommandLineArgs.AssetName}={AssetName}",
            $"{CommandLineArgs.RepoOwner}={RepoOwner}",
            $"{CommandLineArgs.RepoName}={RepoName}",
            DevMode ? CommandLineArgs.DevMode : string.Empty
        }.Where(a => !string.IsNullOrEmpty(a)).ToArray();
    }
}

private static UpdateOptions ParseCommandLineArguments(string[] args)
{
    return new UpdateOptions
    {
        AppName = GetArgValue(args, CommandLineArgs.AppName) ?? string.Empty,
        TargetFolder = GetArgValue(args, CommandLineArgs.Target) ?? string.Empty,
        AssetName = GetArgValue(args, CommandLineArgs.AssetName) ?? string.Empty,
        RepoOwner = GetArgValue(args, CommandLineArgs.RepoOwner) ?? string.Empty,
        RepoName = GetArgValue(args, CommandLineArgs.RepoName) ?? string.Empty,
        DevMode = args.Any(arg => arg.Equals(CommandLineArgs.DevMode, StringComparison.OrdinalIgnoreCase))
    };
}

private static void ValidateOptions(UpdateOptions options)
{
    var errors = options.GetValidationErrors().ToArray();
    if (errors.Length > 0)
    {
        throw new ArgumentException(
            $"Invalid arguments:\n{string.Join("\n", errors)}\n\n" +
            $"Required: {CommandLineArgs.AppName}=<AppName> {CommandLineArgs.Target}=<TargetFolder> " +
            $"{CommandLineArgs.AssetName}=<AssetName> {CommandLineArgs.RepoOwner}=<RepoOwner> {CommandLineArgs.RepoName}=<RepoName>");
    }

    // ターゲットフォルダの書き込み権限を確認
    if (!CheckWritePermission(options.TargetFolder))
    {
        throw new UnauthorizedAccessException($"No write permission to target folder: {options.TargetFolder}");
    }
}
```

#### 3. リソース管理とクリーンアップの改善

```csharp
private static async Task<string> DownloadUpdatePackage(ReleaseInfo releaseInfo)
{
    Console.WriteLine($"Downloading v{releaseInfo.Version} ...");
    
    // 明示的なHTTPクライアントの破棄
    using var httpClient = new HttpClient();
    var userAgent = $"{releaseInfo.RepoOwner} {releaseInfo.RepoName} ({AppConstants.AppVersionString})";
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    
    var zipPath = Path.GetTempFileName();
    try
    {
        using var response = await httpClient.GetAsync(releaseInfo.AssetUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        
        var totalSize = response.Content.Headers.ContentLength ?? -1L;
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        
        // 進捗表示付きでダウンロード
        var buffer = new byte[81920];
        long downloaded = 0;
        int read;
        
        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, read);
            downloaded += read;
            
            if (totalSize > 0)
            {
                var percentage = (int)((downloaded * 100) / totalSize);
                Console.Write($"\rDownloading: {downloaded:#,0}/{totalSize:#,0} bytes ({percentage}%)");
            }
            else
            {
                Console.Write($"\rDownloading: {downloaded:#,0} bytes");
            }
        }
        Console.WriteLine();
        
        return zipPath;
    }
    catch
    {
        // エラー時のクリーンアップ
        if (File.Exists(zipPath))
        {
            try { File.Delete(zipPath); } catch { /* 削除に失敗しても続行 */ }
        }
        throw;
    }
}

private static void CleanupTempFiles()
{
    // テンポラリディレクトリの古いバージョンフォルダを削除
    try
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), AppConstants.AppName, "Updater");
        if (Directory.Exists(tempRoot))
        {
            var currentVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                
            foreach (var versionDir in Directory.GetDirectories(tempRoot))
            {
                var dirName = Path.GetFileName(versionDir);
                if (dirName != currentVersion)
                {
                    try
                    {
                        Directory.Delete(versionDir, recursive: true);
                        Console.WriteLine($"Cleaned up old updater version: {dirName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to cleanup old updater version {dirName}: {ex.Message}");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to cleanup temporary files: {ex.Message}");
    }
}
```
