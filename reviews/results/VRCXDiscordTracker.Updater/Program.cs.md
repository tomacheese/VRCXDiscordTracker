# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker.Updater/Program.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 142行（大規模）

## 概要
アップデータのメインエントリーポイント。コマンドライン引数の解析、自己複製、アップデート処理、エラーハンドリングを実装している。

## 総合評価
**スコア: 6/10**

複雑なアップデート処理を実装しているが、エラーハンドリング、セキュリティ、保守性の観点で改善が必要。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐⭐☆☆
**Good:**
- アップデート処理の論理的な流れ
- 自己複製による安全なアップデート

**Issues:**
- 単一メソッドに処理が集中（SRP違反）
- 異なる責任の混在
- 設定値のハードコーディング

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐☆
**Good:**
- C#命名規約に準拠
- 適切なasync/await使用

**Issues:**
- メソッドが過度に長い
- XMLドキュメンテーション不足

### 3. セキュリティ上の問題 ⭐⭐☆☆☆
**Issues:**
- パスインジェクション攻撃への対策不足
- 実行ファイルパスの検証不十分
- 権限昇格の考慮不足

### 4. パフォーマンスの問題 ⭐⭐⭐☆☆
**Good:**
- 非同期処理の適切な使用

**Issues:**
- 同期的なファイル操作
- プロセス待機でのブロッキング

### 5. 可読性・保守性 ⭐⭐☆☆☆
**Issues:**
- 100行を超える巨大なMainメソッド
- 複雑な条件分岐
- マジックナンバーとハードコーディング

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- 静的メソッドでのテスト困難
- 外部依存への強い結合
- 副作用の大きな処理

## 具体的な問題点と改善提案

### 1. 【重要度：高】メソッド分割とクラス設計の改善
**問題**: 巨大なMainメソッド、責任の混在

**改善案**:
```csharp
internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var updater = new ApplicationUpdater();
            await updater.RunAsync(args);
        }
        catch (Exception ex)
        {
            await HandleFatalErrorAsync(ex);
        }
    }

    private static async Task HandleFatalErrorAsync(Exception ex)
    {
        Console.Error.WriteLine($"Fatal error: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
        Console.Error.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
        
        // ここでログ出力やクラッシュレポート送信も検討
    }
}

internal class ApplicationUpdater
{
    private readonly UpdaterSettings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly IProcessManager _processManager;

    public ApplicationUpdater(UpdaterSettings? settings = null, IFileSystem? fileSystem = null, IProcessManager? processManager = null)
    {
        _settings = settings ?? UpdaterSettings.Default;
        _fileSystem = fileSystem ?? new FileSystemWrapper();
        _processManager = processManager ?? new ProcessManagerWrapper();
    }

    public async Task RunAsync(string[] args)
    {
        DisplayHeader();
        
        var config = ParseArguments(args);
        ValidateArguments(config);
        
        if (await ShouldCopyToTempAndRestart(config))
            return;

        await PerformUpdateAsync(config);
    }

    private async Task<bool> ShouldCopyToTempAndRestart(UpdateConfig config)
    {
        var tempExecutable = GetTempExecutablePath(config);
        var currentExecutable = GetCurrentExecutablePath();
        
        if (IsSameFile(currentExecutable, tempExecutable))
            return false;

        await CopyToTempAndRestartAsync(config, currentExecutable, tempExecutable);
        return true;
    }

    private async Task PerformUpdateAsync(UpdateConfig config)
    {
        using var githubService = new GitHubReleaseService(config.RepoOwner, config.RepoName);
        
        var latestRelease = await githubService.GetLatestReleaseAsync(config.AssetName);
        Console.WriteLine($"Latest version: {latestRelease.Version}");
        
        var downloadPath = await githubService.DownloadWithProgressAsync(latestRelease.AssetUrl);
        
        try
        {
            await StopTargetApplicationAsync(config.AppName);
            await ExtractUpdateAsync(downloadPath, config.Target);
            await StartTargetApplicationAsync(config);
        }
        finally
        {
            await CleanupAsync(downloadPath);
        }
    }
}

public record UpdateConfig(
    string AppName,
    string Target,
    string AssetName,
    string RepoOwner,
    string RepoName)
{
    public static UpdateConfig Parse(string[] args)
    {
        var appName = GetArgValue(args, "--app-name");
        var target = GetArgValue(args, "--target");
        var assetName = GetArgValue(args, "--asset-name");
        var repoOwner = GetArgValue(args, "--repo-owner");
        var repoName = GetArgValue(args, "--repo-name");

        if (string.IsNullOrWhiteSpace(appName) ||
            string.IsNullOrWhiteSpace(target) ||
            string.IsNullOrWhiteSpace(assetName) ||
            string.IsNullOrWhiteSpace(repoOwner) ||
            string.IsNullOrWhiteSpace(repoName))
        {
            throw new ArgumentException("Required arguments missing. Use: --app-name=<AppName> --target=<TargetFolder> --asset-name=<AssetName> --repo-owner=<RepoOwner> --repo-name=<RepoName>");
        }

        return new UpdateConfig(appName, target, assetName, repoOwner, repoName);
    }
}
```

### 2. 【重要度：高】セキュリティの強化
**改善案**:
```csharp
private static void ValidateArguments(UpdateConfig config)
{
    // パスインジェクション対策
    if (Path.IsPathRooted(config.Target) && !IsValidPath(config.Target))
        throw new ArgumentException($"Invalid target path: {config.Target}");

    // 実行ファイル名検証
    if (!IsValidExecutableName(config.AppName))
        throw new ArgumentException($"Invalid application name: {config.AppName}");

    // リポジトリ情報検証
    if (!IsValidRepositoryName(config.RepoOwner) || !IsValidRepositoryName(config.RepoName))
        throw new ArgumentException("Invalid repository information");
}

private static bool IsValidPath(string path)
{
    try
    {
        var fullPath = Path.GetFullPath(path);
        var invalidChars = Path.GetInvalidPathChars();
        return !path.Any(c => invalidChars.Contains(c)) && !path.Contains("..");
    }
    catch
    {
        return false;
    }
}

private static bool IsValidExecutableName(string name)
{
    if (string.IsNullOrWhiteSpace(name)) return false;
    
    var invalidChars = Path.GetInvalidFileNameChars();
    return !name.Any(c => invalidChars.Contains(c)) && 
           name.Length <= 255 && 
           !name.StartsWith('.') &&
           Regex.IsMatch(name, @"^[a-zA-Z0-9._-]+$");
}

private static bool IsValidRepositoryName(string name)
{
    return !string.IsNullOrWhiteSpace(name) && 
           Regex.IsMatch(name, @"^[a-zA-Z0-9._-]+$") && 
           name.Length <= 100;
}
```

### 3. 【重要度：高】エラーハンドリングの改善
**改善案**:
```csharp
private async Task PerformUpdateAsync(UpdateConfig config)
{
    GitHubReleaseService? githubService = null;
    string? downloadPath = null;
    
    try
    {
        githubService = new GitHubReleaseService(config.RepoOwner, config.RepoName);
        
        var latestRelease = await githubService.GetLatestReleaseAsync(config.AssetName);
        Console.WriteLine($"Downloading version {latestRelease.Version}...");
        
        downloadPath = await githubService.DownloadWithProgressAsync(latestRelease.AssetUrl);
        
        Console.WriteLine("Stopping target application...");
        var stoppedProcesses = await _processManager.StopProcessesAsync(config.AppName);
        
        if (stoppedProcesses == 0)
        {
            Console.WriteLine($"Warning: No running instances of {config.AppName} found");
        }

        Console.WriteLine("Extracting update...");
        await ExtractUpdateWithValidationAsync(downloadPath, config.Target);
        
        Console.WriteLine("Starting updated application...");
        await StartApplicationAsync(config);
        
        Console.WriteLine("Update completed successfully");
    }
    catch (HttpRequestException ex)
    {
        throw new UpdateException($"Failed to download update: {ex.Message}", ex);
    }
    catch (UnauthorizedAccessException ex)
    {
        throw new UpdateException($"Permission denied during update: {ex.Message}", ex);
    }
    catch (IOException ex)
    {
        throw new UpdateException($"File operation failed: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
        throw new UpdateException($"Unexpected error during update: {ex.Message}", ex);
    }
    finally
    {
        await CleanupResourcesAsync(githubService, downloadPath);
    }
}

private async Task CleanupResourcesAsync(GitHubReleaseService? githubService, string? downloadPath)
{
    try
    {
        githubService?.Dispose();
        
        if (!string.IsNullOrEmpty(downloadPath) && File.Exists(downloadPath))
        {
            await Task.Run(() => File.Delete(downloadPath));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Cleanup failed: {ex.Message}");
    }
}

public class UpdateException : Exception
{
    public UpdateException(string message) : base(message) { }
    public UpdateException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 4. 【重要度：中】設定の外部化
**改善案**:
```csharp
public class UpdaterSettings
{
    public static readonly UpdaterSettings Default = new();
    
    public TimeSpan ProcessShutdownTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan ProcessKillTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public long MaxDownloadSize { get; init; } = 500 * 1024 * 1024; // 500MB
    public bool AllowForceKill { get; init; } = true;
    public bool CreateBackup { get; init; } = false;
    public string TempFolderName { get; init; } = "Updater";
}
```

### 5. 【重要度：中】ログ機能の追加
**改善案**:
```csharp
internal interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
}

internal class ConsoleLogger : ILogger
{
    public void LogInfo(string message) => Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
    public void LogWarning(string message) => Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
    public void LogError(string message, Exception? exception = null) 
    {
        Console.Error.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        if (exception != null)
            Console.Error.WriteLine(exception.ToString());
    }
}
```

## 推奨されるNext Steps
1. クラス設計の改善とメソッド分割（高優先度）
2. セキュリティ検証の強化（高優先度）
3. 包括的なエラーハンドリング（高優先度）
4. 設定クラスの導入（中優先度）
5. ログ機能の実装（中優先度）
6. 単体テストの追加（中優先度）

## コメント
アップデータの複雑な処理を実装していますが、単一メソッドに処理が集中しており保守性に問題があります。セキュリティ脆弱性も深刻で、特にパスインジェクション対策とファイル操作の安全性確保が急務です。クラス設計を見直し、責任を適切に分離することで、より安全で保守しやすいコードにする必要があります。