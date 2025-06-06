# VRCXDiscordTracker/Program.cs レビュー結果

## ファイル概要

アプリケーションのエントリーポイント。システムトレイアプリケーションとして動作し、例外ハンドリング、アップデートチェック、通知機能を統合。

## 評価項目

### 1. 設計・構造

#### 良い点

- 適切な例外ハンドリングの実装
- システムトレイアプリケーションとしての構成
- モジュラーな設計（TrayIcon、Controller分離）

#### 改善点

- Mainメソッドが複雑すぎる（責任が多い）
- グローバル状態（Controller）の使用

### 2. 例外ハンドリング

#### 良い点

- 包括的な例外キャッチ（ThreadException, UnhandledException, UnobservedTaskException）
- ユーザーフレンドリーなエラーダイアログ
- GitHub Issues自動作成機能

#### 改善点

1. 例外ログの出力先が標準出力のみ
   ```csharp
   // 推奨: 構造化ログの使用
   private static readonly ILogger Logger = LoggerFactory.Create(builder =>
       builder.AddConsole().AddFile("logs/app-{Date}.log")).CreateLogger<Program>();
   ```

2. 例外の分類と処理の改善
   ```csharp
   public static void OnException(Exception e, string exceptionType)
   {
       Logger.LogError(e, "Unhandled exception: {ExceptionType}", exceptionType);
       
       // 重要でない例外は自動で報告せず、ログのみ
       if (IsCriticalException(e))
       {
           ShowErrorDialog(e, exceptionType);
       }
       else
       {
           Logger.LogWarning("Non-critical exception handled: {Message}", e.Message);
       }
   }
   ```

### 3. アプリケーションライフサイクル

#### 良い点

- トースト通知からの起動への対応
- ApplicationExitイベントでのクリーンアップ

#### 改善点

1. Task.Wait()の使用が危険
   ```csharp
   // 現在の問題のあるコード
   Task.Run(async () => {
       var existsUpdate = await UpdateChecker.Check();
       // ...
   }).Wait(); // デッドロックのリスク
   
   // 推奨: 非同期Mainメソッドの使用
   static async Task Main()
   {
       // ...
       if (!cmds.Any(cmd => cmd.Equals("--skip-update")))
       {
           var existsUpdate = await UpdateChecker.Check();
           if (existsUpdate)
           {
               Console.WriteLine("Found update. Exiting...");
               return;
           }
       }
       // ...
   }
   ```

2. アプリケーション終了時の非同期処理
   ```csharp
   Application.ApplicationExit += async (s, e) =>
   {
       try
       {
           if (AppConfig.NotifyOnExit)
           {
               // タイムアウト付きで実行
               using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
               await DiscordNotificationService.SendAppExitMessage()
                   .WaitAsync(cts.Token);
           }
       }
       catch (Exception ex)
       {
           Logger.LogWarning(ex, "Failed to send exit notification");
       }
       finally
       {
           Controller?.Dispose();
           ToastNotificationManagerCompat.Uninstall();
       }
   };
   ```

### 4. セキュリティ

#### 良い点

- P/Invokeの適切な使用（LibraryImport）

#### 改善点

1. コマンドライン引数の検証
   ```csharp
   private static void ValidateCommandLineArgs(string[] args)
   {
       var allowedArgs = new[] { "--debug", "--skip-update" };
       var invalidArgs = args.Where(arg => arg.StartsWith("--") && !allowedArgs.Contains(arg));
       
       if (invalidArgs.Any())
       {
           Console.WriteLine($"Invalid arguments: {string.Join(", ", invalidArgs)}");
           Environment.Exit(1);
       }
   }
   ```

### 5. コードの可読性・保守性

#### 改善点

1. Mainメソッドの分割
   ```csharp
   static async Task Main()
   {
       try
       {
           await InitializeApplicationAsync();
       }
       catch (Exception ex)
       {
           OnException(ex, "ApplicationInitialization");
       }
   }
   
   private static async Task InitializeApplicationAsync()
   {
       ConfigureGlobalExceptionHandling();
       ConfigureConsoleOutput();
       
       await CheckForUpdatesAsync();
       
       InitializeApplication();
       StartApplicationServices();
       
       Application.Run(new TrayIcon());
   }
   ```

2. 設定の中央集約
   ```csharp
   private static void InitializeApplication()
   {
       ApplicationConfiguration.Initialize();
       
       // 設定の検証
       if (!AppConfig.IsValid)
       {
           ShowInitialSetup();
           return;
       }
       
       StartServices();
   }
   ```

### 6. パフォーマンス

#### 改善点

1. StringBuilder の効率的な使用
   ```csharp
   private static string GetErrorDetails(Exception e, bool isMarkdown)
   {
       // 初期容量を指定してメモリ効率を向上
       var sb = new StringBuilder(1024);
       // ...
   }
   ```

### 7. ログ・監視

#### 改善点

1. 構造化ログの導入
   ```csharp
   // NuGet: Serilog.Extensions.Hosting, Serilog.Sinks.File
   private static void ConfigureLogging()
   {
       Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .WriteTo.Console()
           .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
           .CreateLogger();
   }
   ```

### 8. 完全な推奨実装

```csharp
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;
using VRCXDiscordTracker.Core;
using VRCXDiscordTracker.Core.Config;
using VRCXDiscordTracker.Core.Notification;
using VRCXDiscordTracker.Core.UI.TrayIcon;
using VRCXDiscordTracker.Core.Updater;

namespace VRCXDiscordTracker;

internal static partial class Program
{
    private static ILogger<Program>? _logger;
    private static VRCXDiscordTrackerController? _controller;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [STAThread]
    static async Task Main(string[] args)
    {
        try
        {
            ConfigureLogging();
            _logger = LoggerFactory.Create(builder => builder.AddSerilog()).CreateLogger<Program>();
            
            ValidateCommandLineArgs(args);
            await InitializeApplicationAsync(args);
        }
        catch (Exception ex)
        {
            await OnExceptionAsync(ex, "ApplicationStartup");
            Environment.Exit(1);
        }
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    private static void ValidateCommandLineArgs(string[] args)
    {
        var allowedArgs = new[] { "--debug", "--skip-update" };
        var invalidArgs = args.Where(arg => arg.StartsWith("--") && !allowedArgs.Contains(arg));
        
        if (invalidArgs.Any())
        {
            throw new ArgumentException($"Invalid arguments: {string.Join(", ", invalidArgs)}");
        }
    }

    private static async Task InitializeApplicationAsync(string[] args)
    {
        if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            ToastNotificationManagerCompat.Uninstall();
            return;
        }

        ConfigureGlobalExceptionHandling();
        ConfigureConsoleOutput(args);
        
        await CheckForUpdatesAsync(args);
        
        ApplicationConfiguration.Initialize();
        InitializeServices();
        
        Application.Run(new TrayIcon());
    }

    private static void ConfigureGlobalExceptionHandling()
    {
        Application.ThreadException += async (s, e) => await OnExceptionAsync(e.Exception, "ThreadException");
        AppDomain.CurrentDomain.UnhandledException += async (s, e) => await OnExceptionAsync((Exception)e.ExceptionObject, "UnhandledException");
        TaskScheduler.UnobservedTaskException += async (s, e) => await OnExceptionAsync(e.Exception, "UnobservedTaskException");
    }

    private static void ConfigureConsoleOutput(string[] args)
    {
        if (args.Contains("--debug"))
        {
            AllocConsole();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.OutputEncoding = Encoding.UTF8;
        }
    }

    private static async Task CheckForUpdatesAsync(string[] args)
    {
        if (args.Contains("--skip-update"))
        {
            _logger?.LogInformation("Skipping update check");
            return;
        }

        try
        {
            var hasUpdate = await UpdateChecker.Check();
            if (hasUpdate)
            {
                _logger?.LogInformation("Update found, exiting for update");
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to check for updates, continuing without update");
        }
    }

    private static void InitializeServices()
    {
        _controller = new VRCXDiscordTrackerController(AppConfig.DatabasePath);
        
        if (string.IsNullOrEmpty(AppConfig.DiscordWebhookUrl))
        {
            ShowInitialSetup();
        }
        else
        {
            StartServices();
        }
        
        ConfigureApplicationExit();
    }

    private static void ShowInitialSetup()
    {
        // 初期設定画面の表示
        var trayIcon = new TrayIcon();
        trayIcon.OpenSettingsWindow();
    }

    private static void StartServices()
    {
        _controller?.Start();
        
        if (AppConfig.NotifyOnStart)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await DiscordNotificationService.SendAppStartMessage();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to send start notification");
                }
            });
        }
    }

    private static void ConfigureApplicationExit()
    {
        Application.ApplicationExit += async (s, e) =>
        {
            try
            {
                if (AppConfig.NotifyOnExit)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await DiscordNotificationService.SendAppExitMessage()
                        .WaitAsync(cts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send exit notification");
            }
            finally
            {
                _controller?.Dispose();
                ToastNotificationManagerCompat.Uninstall();
                Log.CloseAndFlush();
            }
        };
    }

    public static async Task OnExceptionAsync(Exception e, string exceptionType)
    {
        _logger?.LogError(e, "Unhandled exception: {ExceptionType}", exceptionType);
        
        if (IsCriticalException(e))
        {
            await ShowErrorDialogAsync(e, exceptionType);
        }
    }

    private static bool IsCriticalException(Exception e)
    {
        return e is not (TaskCanceledException or OperationCanceledException);
    }

    private static async Task ShowErrorDialogAsync(Exception e, string exceptionType)
    {
        var errorDetails = GetErrorDetails(e, false);
        var message = string.Join("\n", new[]
        {
            "An error has occurred and the operation has stopped.",
            "It would be helpful if you could report this bug using GitHub issues!",
            $"https://github.com/tomacheese/{AppConstants.AppName}/issues",
            "",
            errorDetails,
            "",
            "Click OK to open the Create GitHub issue page.",
            "Click Cancel to close this application."
        });

        var result = MessageBox.Show(message, $"Error ({exceptionType})", 
            MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

        if (result == DialogResult.OK)
        {
            var issueBody = Uri.EscapeDataString(GetErrorDetails(e, true));
            var url = $"https://github.com/tomacheese/{AppConstants.AppName}/issues/new?body={issueBody}";
            
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        
        Application.Exit();
    }

    private static string GetErrorDetails(Exception e, bool isMarkdown)
    {
        var sb = new StringBuilder(1024);
        var fence = isMarkdown ? "```" : string.Empty;

        void AppendSection(string title, string content)
        {
            if (isMarkdown)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"## {title}\n\n{fence}\n{content}\n{fence}\n");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"----- {title} -----\n{content}\n");
            }
        }

        var current = e;
        var level = 0;
        while (current != null)
        {
            var title = level == 0 ? "Error" : $"Inner Exception (Level {level})";
            AppendSection(title, $"{current.Message ?? "<no message>"}\n{current.StackTrace ?? "<no trace>"}");
            
            current = current.InnerException;
            level++;
        }

        AppendSection("Environment",
            $"OS: {Environment.OSVersion}\n" +
            $"CLR: {Environment.Version}\n" +
            $"App: {AppConstants.AppName} {AppConstants.AppVersionString}");

        return sb.ToString().Trim();
    }
}
```

## 総合評価

アプリケーションのエントリーポイントとして基本的な機能は実装されているが、現代的なC#アプリケーションのベストプラクティスと比較して改善の余地が多い。特に、非同期処理の不適切な使用、例外ハンドリングの改善、ログ機能の強化、Mainメソッドの責任分散が重要。また、依存性注入とホストサービスパターンの導入により、より保守性の高いアーキテクチャを実現できる。