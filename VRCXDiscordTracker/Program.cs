using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
        };

        Application.Run(trayIcon);
    }

    public static void OnException(Exception e, string exceptionType)
    {
        Console.WriteLine($"Exception: {exceptionType}");
        Console.WriteLine($"Message: {e.Message}");
        Console.WriteLine($"InnerException: {e.InnerException?.Message}");
        Console.WriteLine($"StackTrace: {e.StackTrace}");

        var errorDetailAndStacktrace = "----- Error Details -----\n" +
            e.Message + "\n" +
            e.InnerException?.Message + "\n" +
            "\n" +
            "----- StackTrace -----\n" +
            e.StackTrace + "\n";

        DialogResult result = MessageBox.Show(
            "An error has occurred and the operation has stopped.\n" +
            "It would be helpful if you could report this bug using GitHub issues!\n" +
            "https://github.com/tomacheese/" + AppConstants.AppName + "/issues\n" +
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
            Process.Start(new ProcessStartInfo()
            {
                FileName = "https://github.com/tomacheese/" + AppConstants.AppName + "/issues/new?body=" + Uri.EscapeDataString(errorDetailAndStacktrace),
                UseShellExecute = true,
            });
        }
        Application.Exit();
    }
}