using System;
using System.Windows.Forms;

namespace VRCXDiscordTracker
{
    public partial class SettingsForm : Form
    {
        private string lastSavedDatabasePath;
        private string lastSavedDiscordWebhookUrl;

        public SettingsForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 設定画面がロードされたときの処理
        /// </summary>
        private void OnLoad(object sender, EventArgs e)
        {
            // 設定ファイルから値を読み込む
            textBoxDatabasePath.Text = AppConfig.DatabasePath ?? Program.Watcher.GetDatabasePath(); // 設定ファイルで規定していない場合は実際の監視対象を取得
            textBoxDiscordWebhookUrl.Text = AppConfig.DiscordWebhookUrl;

            lastSavedDatabasePath = textBoxDatabasePath.Text;
            lastSavedDiscordWebhookUrl = textBoxDiscordWebhookUrl.Text;
        }

        private bool Save()
        {
            try
            {
                AppConfig.DatabasePath = !string.IsNullOrEmpty(textBoxDatabasePath.Text) ? textBoxDatabasePath.Text : null;
                AppConfig.DiscordWebhookUrl = !string.IsNullOrEmpty(textBoxDiscordWebhookUrl.Text) ? textBoxDiscordWebhookUrl.Text : null;

                Program.Watcher.Stop();
                Program.Watcher = new VRCXDatabaseWatcher(textBoxDatabasePath.Text);
                Program.Watcher.Start();

                lastSavedDatabasePath = textBoxDatabasePath.Text;
                lastSavedDiscordWebhookUrl = textBoxDiscordWebhookUrl.Text;

                Notifier.ShowToast("Settings Saved", "Settings have been saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 保存ボタンがクリックされたときの処理
        /// </summary>
        /// <remarks>設定ファイルに値を保存し、VRChatのログ監視を再起動する</remarks>
        private void OnSaveButtonClicked(object sender, EventArgs e)
        {
            Save();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            var changed = lastSavedDatabasePath != textBoxDatabasePath.Text.Trim() || lastSavedDiscordWebhookUrl != textBoxDiscordWebhookUrl.Text.Trim();
            if (!changed)
            {
                return;
            }

            var result = MessageBox.Show("Some settings are not saved. Do you want to save them?", "Confirm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                var saved = Save();
                if (!saved)
                {
                    e.Cancel = true; // 保存に失敗した場合は閉じない
                }
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }
    }
}
