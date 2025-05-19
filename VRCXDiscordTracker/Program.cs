using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Toolkit.Uwp.Notifications;
using VRCXDiscordTracker.Core;
using VRCXDiscordTracker.Core.Config;
using VRCXDiscordTracker.Core.Notification;
using VRCXDiscordTracker.Core.UI.TrayIcon;

namespace VRCXDiscordTracker;

internal static partial class Program
{
    public static VRCXDiscordTrackerController? Controller;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [STAThread]
    static void Main()
    {
        if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            // トースト通知から起動された場合、なにもしない
            ToastNotificationManagerCompat.Uninstall();
            return;
        }

        Application.ThreadException += (s, e) => OnException(e.Exception, "ThreadException");
        Thread.GetDomain().UnhandledException += (s, e) => OnException((Exception)e.ExceptionObject, "UnhandledException");
        TaskScheduler.UnobservedTaskException += (s, e) => OnException(e.Exception, "UnobservedTaskException");

        var cmds = Environment.GetCommandLineArgs();
        if (cmds.Any(cmd => cmd.Equals("--debug")))
        {
            AllocConsole();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.OutputEncoding = Encoding.UTF8;
        }

        Console.WriteLine("Program.Main");

        ApplicationConfiguration.Initialize();

        var trayIcon = new TrayIcon();

        Controller = new VRCXDiscordTrackerController(AppConfig.DatabasePath);

        // DiscordWebhookUrlが空の場合は、設定画面を表示する
        if (string.IsNullOrEmpty(AppConfig.DiscordWebhookUrl))
        {
            trayIcon.OpenSettingsWindow();
        }
        else
        {
            Controller.Start();

            if (AppConfig.NotifyOnStart)
            {
                DiscordNotificationService.SendAppStartMessage().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Console.WriteLine($"Error sending app start message: {t.Exception?.Message}");
                    }
                });
            }
        }

        Application.ApplicationExit += async (s, e) =>
        {
            if (AppConfig.NotifyOnExit)
            {
                await DiscordNotificationService.SendAppExitMessage();
            }
            Controller?.Dispose();
            ToastNotificationManagerCompat.Uninstall();
        };

        Application.Run(trayIcon);
    }

    public static void OnException(Exception e, string exceptionType)
    {
        Console.WriteLine($"Exception: {exceptionType}");
        Console.WriteLine($"Message: {e.Message}");
        Console.WriteLine($"InnerException: {e.InnerException?.Message}");
        Console.WriteLine($"StackTrace: {e.StackTrace}");
        Console.WriteLine($"InnerException StackTrace: {e.InnerException?.StackTrace}");

        DialogResult result = MessageBox.Show(
            "An error has occurred and the operation has stopped.\n" +
            "It would be helpful if you could report this bug using GitHub issues!\n" +
            "https://github.com/tomacheese/" + AppConstants.AppName + "/issues\n" +
            "\n" +
            GetErrorDetails(e, false) +
            "\n\n" +
            "Click OK to open the Create GitHub issue page.\n" +
            "Click Cancel to close this application.",
            $"Error ({exceptionType})",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Error);

        if (result == DialogResult.OK)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "https://github.com/tomacheese/" + AppConstants.AppName + "/issues/new?body=" + Uri.EscapeDataString(GetErrorDetails(e, true)),
                UseShellExecute = true,
            });
        }
        Application.Exit();
    }

    private static string GetErrorDetails(Exception e, bool isMarkdown)
    {
        var sb = new StringBuilder();
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

        Exception? current = e;
        var level = 0;
        while (current != null)
        {
            var title = level == 0
                ? "Error"
                : $"Inner Exception (Level {level})";
            AppendSection(title,
                (current.Message ?? "<no message>") + "\n" +
                (current.StackTrace ?? "<no trace>"));

            current = current.InnerException;
            level++;
        }

        // Environment info
        AppendSection("Environment",
            $"OS: {Environment.OSVersion}\n" +
            $"CLR: {Environment.Version}\n" +
            $"App: {AppConstants.AppName} {AppConstants.AppVersion}");

        return sb.ToString().Trim();
    }
}