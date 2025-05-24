using VRCXDiscordTracker.Core.Config;
using VRCXDiscordTracker.Core.Notification;

namespace VRCXDiscordTracker.Core;

/// <summary>
/// 設定画面
/// </summary>
internal partial class SettingsForm : Form
{
    /// <summary>
    /// 最後に保存したデータベースのパス
    /// </summary>
    private string _lastSavedDatabasePath = string.Empty;

    /// <summary>
    /// 最後に保存したDiscordのWebhook URL
    /// </summary>
    private string _lastSavedDiscordWebhookUrl = string.Empty;

    /// <summary>
    /// 最後に保存した起動時通知の設定
    /// </summary>
    private bool _lastSavedNotifyOnStart = true;

    /// <summary>
    /// 最後に保存した終了時通知の設定
    /// </summary>
    private bool _lastSavedNotifyOnExit = true;

    public SettingsForm() => InitializeComponent();

    /// <summary>
    /// 設定画面がロードされたときの処理
    /// </summary>
    private void OnLoad(object sender, EventArgs e)
    {
        // 設定ファイルから値を読み込む
        textBoxDatabasePath.Text = AppConfig.DatabasePath;
        // 設定ファイルで規定していない場合は実際の監視対象を取得
        if (string.IsNullOrWhiteSpace(textBoxDatabasePath.Text))
        {
            textBoxDatabasePath.Text = Program.Controller?.GetDatabasePath() ?? string.Empty;
        }
        textBoxDiscordWebhookUrl.Text = AppConfig.DiscordWebhookUrl;
        checkBoxNotifyOnStart.Checked = AppConfig.NotifyOnStart;
        checkBoxNotifyOnExit.Checked = AppConfig.NotifyOnExit;

        _lastSavedDatabasePath = textBoxDatabasePath.Text;
        _lastSavedDiscordWebhookUrl = textBoxDiscordWebhookUrl.Text;
        _lastSavedNotifyOnStart = AppConfig.NotifyOnStart;
        _lastSavedNotifyOnExit = AppConfig.NotifyOnExit;
    }

    /// <summary>
    /// 設定を保存するメソッド
    /// </summary>
    /// <returns>保存に成功した場合はtrue、失敗した場合はfalse</returns>
    private bool Save()
    {
        try
        {
            AppConfig.DatabasePath = textBoxDatabasePath.Text;
            AppConfig.DiscordWebhookUrl = textBoxDiscordWebhookUrl.Text;
            AppConfig.NotifyOnStart = checkBoxNotifyOnStart.Checked;
            AppConfig.NotifyOnExit = checkBoxNotifyOnExit.Checked;

            Program.Controller?.Dispose();
            Program.Controller = new VRCXDiscordTrackerController(textBoxDatabasePath.Text);
            Program.Controller.Start();

            _lastSavedDatabasePath = textBoxDatabasePath.Text;
            _lastSavedDiscordWebhookUrl = textBoxDiscordWebhookUrl.Text;
            _lastSavedNotifyOnStart = checkBoxNotifyOnStart.Checked;
            _lastSavedNotifyOnExit = checkBoxNotifyOnExit.Checked;

            UwpNotificationService.Notify("Settings Saved", "Settings have been saved successfully.");
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
        var textBoxDiscordWebhookUrlText = textBoxDiscordWebhookUrl.Text.Trim();
        if (string.IsNullOrEmpty(textBoxDiscordWebhookUrlText))
        {
            MessageBox.Show("Discord Webhook URL is required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Save();
    }

    /// <summary>
    /// フォームが閉じられるときの処理。
    /// DiscordのWebhook URLが空の場合は、アプリケーションを終了する。
    /// 未保存の設定がある場合は、保存するかどうかを確認する。
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">FormClosingEventArgs</param>
    private void OnFormClosing(object sender, FormClosingEventArgs e)
    {
        var textBoxDiscordWebhookUrlText = textBoxDiscordWebhookUrl.Text.Trim();
        if (string.IsNullOrEmpty(textBoxDiscordWebhookUrlText))
        {
            Application.Exit(); // メッセージ表示後にExitを呼び出すと2回メッセージが表示されてしまうので先に呼び出す
            MessageBox.Show("Discord Webhook URL is required. Application will be closed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var changed = _lastSavedDatabasePath != textBoxDatabasePath.Text.Trim() ||
                      _lastSavedDiscordWebhookUrl != textBoxDiscordWebhookUrlText ||
                      _lastSavedNotifyOnStart != checkBoxNotifyOnStart.Checked ||
                      _lastSavedNotifyOnExit != checkBoxNotifyOnExit.Checked;
        if (!changed)
        {
            return;
        }

        DialogResult result = MessageBox.Show("Some settings are not saved. Do you want to save them?", "Confirm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
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
