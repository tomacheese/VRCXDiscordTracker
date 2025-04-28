using System;
using System.IO;
using System.Runtime.InteropServices;
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
    }
}