using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VRCXDiscordTracker
{
    internal static class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);



        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public static VRCXDatabaseWatcher Watcher { get; set; }

        [STAThread]
        static void Main()
        {
            Application.ThreadException += new
                ThreadExceptionEventHandler(Application_ThreadException);

            // UnhandledExceptionイベント・ハンドラを登録する
            Thread.GetDomain().UnhandledException += new
                UnhandledExceptionEventHandler(Application_UnhandledException);

            string[] cmds = Environment.GetCommandLineArgs();
            bool isDebugMode = false;
            foreach (string cmd in cmds)
            {
                if (cmd.Equals("--debug"))
                {
                    isDebugMode = true;
                }
            }
            if (isDebugMode)
            {
                AllocConsole();
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }

            SetCurrentProcessExplicitAppUserModelID("Tomacheese.VRCXDiscordTracker");

            // VRChatのログを監視
            string targetDatabasePath = AppConfig.DatabasePath;
            Watcher = new VRCXDatabaseWatcher(targetDatabasePath);
            Watcher.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VRCXDiscordTracker());
        }


        public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "An error has occurred and the operation has stopped.\n" +
                "It would be helpful if you could report this bug using GitHub issues!\n" +
                "https://github.com/tomacheese/VRCXDiscordTracker/issues\n" +
                "\n" +
                "----- Error Details -----\n" +
                e.Exception.Message + "\n" +
                "\n" +
                "----- StackTrace -----\n" +
                e.Exception.StackTrace + "\n" +
                "\n" +
                "Click OK to open the Create GitHub issue page.\n" +
                "Click Cancel to close this application.",
                "Error (ThreadException)",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Error);

            if (result == DialogResult.OK)
            {
                Process.Start("https://github.com/tomacheese/VRCXDiscordTracker/issues/new");
            }
            Application.Exit();
        }

        public static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                DialogResult result = MessageBox.Show(
                    "An error has occurred and the operation has stopped.\n" +
                    "It would be helpful if you could report this bug using GitHub issues!\n" +
                    "https://github.com/tomacheese/VRCXDiscordTracker/issues\n" +
                    "\n" +
                    "----- Error Details -----\n" +
                    ex.Message + "\n" +
                    "\n" +
                    "----- StackTrace -----\n" +
                    ex.StackTrace + "\n" +
                    "\n" +
                    "Click OK to open the Create GitHub issue page.\n" +
                    "Click Cancel to close this application.",
                    "Error (UnhandledException)",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Error);

                if (result == DialogResult.OK)
                {
                    Process.Start("https://github.com/tomacheese/VRCXDiscordTracker/issues/new");
                }
                Application.Exit();
            }
        }
    }
}