using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VRCXDiscordTracker
{
    internal static class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        public static VRCXDatabaseWatcher Watcher { get; set; }

        [STAThread]
        static void Main()
        {
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