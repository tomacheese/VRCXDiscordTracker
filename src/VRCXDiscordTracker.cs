using System;
using System.Windows.Forms;

namespace VRCXDiscordTracker
{
    internal class VRCXDiscordTracker : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private SettingsForm settingsForm;

        public VRCXDiscordTracker()
        {
            // システムトレイアイコンの設定
            trayIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.AppIcon,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Settings", ShowSettings),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true,
                Text = "VRCXDiscordTracker"
            };
            // トレイアイコンのクリックイベントを設定。クリックで設定画面を表示
            trayIcon.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowSettings(sender, e);
                }
            };
        }

        /// <summary>
        /// 設定画面を表示する
        /// </summary>
        private void ShowSettings(object sender, EventArgs e)
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm();
            }

            settingsForm.Show();
            settingsForm.BringToFront();
        }

        /// <summary>
        /// アプリケーションを終了する
        /// </summary>
        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }
    }
}
