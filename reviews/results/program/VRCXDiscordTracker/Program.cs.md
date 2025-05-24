# Program.cs レビュー

## 概要

`Program.cs`はVRCXDiscordTrackerアプリケーションのエントリーポイントで、アプリケーションの初期設定と実行フローを制御しています。

## 良い点

1. 例外処理が包括的に実装されており、3種類の例外（Thread、Domain、Task）を捕捉する仕組みがある
2. コマンドライン引数による動作切り替え（`--debug`、`--skip-update`）が実装されている
3. エラー発生時にユーザーフレンドリーなエラーダイアログを表示し、GitHubイシュー報告を促進している
4. アプリケーション終了時のクリーンアップ処理が実装されている

## 改善点

### 1. 例外処理メソッドの分離

`OnException`メソッドがかなり長く、複数の責務を持っています。エラーログ出力とユーザー通知を分離すべきです。

```csharp
public static void OnException(Exception e, string exceptionType)
{
    // ログ出力部分
    LogException(e, exceptionType);
    
    // ユーザー通知部分
    ShowExceptionDialog(e, exceptionType);
}

private static void LogException(Exception e, string exceptionType)
{
    Console.WriteLine($"Exception: {exceptionType}");
    Console.WriteLine($"Message: {e.Message}");
    Console.WriteLine($"InnerException: {e.InnerException?.Message}");
    Console.WriteLine($"StackTrace: {e.StackTrace}");
}

private static void ShowExceptionDialog(Exception e, string exceptionType)
{
    var errorDetailAndStacktrace = BuildErrorDetails(e);
    
    DialogResult result = MessageBox.Show(
        "An error has occurred and the operation has stopped.\n" +
        "It would be helpful if you could report this bug using GitHub issues!\n" +
        $"https://github.com/{AppConstants.GitHubRepoOwner}/{AppConstants.GitHubRepoName}/issues\n" +
        "\n" +
        errorDetailAndStacktrace +
        "\n" +
        "Click OK to open the Create GitHub issue page.\n" +
        "Click Cancel to close this application.",
        $"Error ({exceptionType})",
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Error);

    if (result == DialogResult.OK)
    {
        OpenGitHubIssue(errorDetailAndStacktrace);
    }
    Application.Exit();
}

private static string BuildErrorDetails(Exception e)
{
    return "----- Error Details -----\n" +
        e.Message + "\n" +
        e.InnerException?.Message + "\n" +
        "\n" +
        "----- StackTrace -----\n" +
        e.StackTrace + "\n";
}

private static void OpenGitHubIssue(string errorDetails)
{
    Process.Start(new ProcessStartInfo()
    {
        FileName = $"https://github.com/{AppConstants.GitHubRepoOwner}/{AppConstants.GitHubRepoName}/issues/new?body=" + 
            Uri.EscapeDataString(errorDetails),
        UseShellExecute = true,
    });
}
```

### 2. GitHubリポジトリ情報の管理

GitHubリポジトリ情報がハードコードされている箇所があります。`AppConstants`クラスで定義されている値を使用すべきです。

```diff
- FileName = "https://github.com/tomacheese/" + AppConstants.AppName + "/issues/new?body=" + Uri.EscapeDataString(errorDetailAndStacktrace),
+ FileName = $"https://github.com/{AppConstants.GitHubRepoOwner}/{AppConstants.GitHubRepoName}/issues/new?body=" + Uri.EscapeDataString(errorDetailAndStacktrace),
```

### 3. コマンドライン引数処理の改善

コマンドライン引数の処理がシンプルですが、今後の拡張性を考慮して、より構造化された処理にすると良いでしょう。

```csharp
private static bool ParseCommandLineArguments(string[] args, out CommandLineOptions options)
{
    options = new CommandLineOptions();
    
    foreach (var arg in args)
    {
        switch (arg.ToLowerInvariant())
        {
            case "--debug":
                options.IsDebugMode = true;
                break;
            case "--skip-update":
                options.SkipUpdate = true;
                break;
            // 将来追加される可能性のあるオプション
            default:
                // 未知のオプションの処理
                break;
        }
    }
    
    return true;
}

private class CommandLineOptions
{
    public bool IsDebugMode { get; set; }
    public bool SkipUpdate { get; set; }
    // 他のオプションを追加可能
}
```

### 4. メソッドの責務分離

`Main`メソッドが長く、複数の責務を持っているため、初期化、更新チェック、アプリケーション起動などのロジックを分離すると良いでしょう。

```csharp
[STAThread]
static async Task Main()
{
    if (HandleToastActivation())
        return;

    SetupExceptionHandlers();
    
    var options = ParseCommandLineArguments(Environment.GetCommandLineArgs());
    
    if (options.IsDebugMode)
        EnableConsole();
    
    Console.WriteLine("Program.Main");
    
    if (!options.SkipUpdate && await CheckForUpdates())
        return;
    
    await RunApplication();
}

private static bool HandleToastActivation()
{
    if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
    {
        // トースト通知から起動された場合、なにもしない
        ToastNotificationManagerCompat.Uninstall();
        return true;
    }
    return false;
}

private static void SetupExceptionHandlers()
{
    Application.ThreadException += (s, e) => OnException(e.Exception, "ThreadException");
    Thread.GetDomain().UnhandledException += (s, e) => OnException((Exception)e.ExceptionObject, "UnhandledException");
    TaskScheduler.UnobservedTaskException += (s, e) => OnException(e.Exception, "UnobservedTaskException");
}

private static void EnableConsole()
{
    AllocConsole();
    Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    Console.OutputEncoding = Encoding.UTF8;
}

private static async Task<bool> CheckForUpdates()
{
    var existsUpdate = await UpdateChecker.Check();
    if (existsUpdate)
    {
        Console.WriteLine("Found update. Exiting...");
        return true;
    }
    return false;
}

private static async Task RunApplication()
{
    ApplicationConfiguration.Initialize();
    
    var trayIcon = new TrayIcon();
    
    Controller = new VRCXDiscordTrackerController(AppConfig.DatabasePath);
    
    if (string.IsNullOrEmpty(AppConfig.DiscordWebhookUrl))
    {
        trayIcon.OpenSettingsWindow();
    }
    else
    {
        Controller.Start();
        
        if (AppConfig.NotifyOnStart)
        {
            await SendStartupNotification();
        }
    }
    
    SetupApplicationExit();
    
    Application.Run(trayIcon);
}

private static async Task SendStartupNotification()
{
    await DiscordNotificationService.SendAppStartMessage().ContinueWith(t =>
    {
        if (t.IsFaulted)
        {
            Console.WriteLine($"Error sending app start message: {t.Exception?.Message}");
        }
    });
}

private static void SetupApplicationExit()
{
    Application.ApplicationExit += async (s, e) =>
    {
        if (AppConfig.NotifyOnExit)
        {
            await DiscordNotificationService.SendAppExitMessage();
        }
        Controller?.Dispose();
        ToastNotificationManagerCompat.Uninstall();
    };
}
```

### 5. 非同期処理のエラーハンドリング

アプリケーション終了時の非同期処理でエラーハンドリングが不足しています。

```diff
Application.ApplicationExit += async (s, e) =>
{
    if (AppConfig.NotifyOnExit)
    {
-       await DiscordNotificationService.SendAppExitMessage();
+       try
+       {
+           await DiscordNotificationService.SendAppExitMessage();
+       }
+       catch (Exception ex)
+       {
+           Console.WriteLine($"Error sending app exit message: {ex.Message}");
+       }
    }
    Controller?.Dispose();
    ToastNotificationManagerCompat.Uninstall();
};
```

### 6. `Controller` の public static 宣言について

`Controller`インスタンスが公開されており、外部からアクセス可能な状態です。カプセル化の観点から見直しが必要です。

```diff
- public static VRCXDiscordTrackerController? Controller;
+ private static VRCXDiscordTrackerController? Controller;
```

必要に応じてアクセサメソッドを提供するか、グローバルな状態管理を見直すことを検討すべきです。

## セキュリティ上の懸念点

例外発生時にスタックトレースなどの詳細情報がユーザーに表示されます。これは開発中やデバッグ時には有用ですが、実運用環境では情報漏洩のリスクになる可能性があります。デバッグモードと本番モードで表示内容を分けることも検討すべきです。

## 総合評価

全体的に機能面では十分に実装されていますが、コード構造と責務分離の面で改善の余地があります。特に例外処理とコマンドライン引数の処理、そして`Main`メソッドの責務分離を行うことで、コードの保守性と拡張性が向上するでしょう。
