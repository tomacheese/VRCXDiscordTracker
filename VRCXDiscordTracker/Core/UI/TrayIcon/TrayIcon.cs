namespace VRCXDiscordTracker.Core.UI.TrayIcon;
internal class TrayIcon : ApplicationContext
{
    /// <summary>
    /// トレイアイコン
    /// </summary>
    private readonly NotifyIcon _trayIcon = new();

    /// <summary>
    /// 設定画面
    /// </summary>
    private SettingsForm _settingsForm = new();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public TrayIcon()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Settings", null, ShowSettings);
        contextMenu.Items.Add("Exit", null, Exit);

        _trayIcon.Icon = Properties.Resources.AppIcon;
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.Text = AppConstants.AppName;
        _trayIcon.Visible = true;
        _trayIcon.MouseClick += (sender, e) =>
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
    public void OpenSettingsWindow()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm();
        }
        _settingsForm.Show();
        _settingsForm.BringToFront();
    }

    /// <summary>
    /// Settingsボタンがクリックされたときの処理
    /// </summary>
    private void ShowSettings(object? sender, EventArgs e) => OpenSettingsWindow();

    /// <summary>
    /// アプリケーションを終了する
    /// </summary>
    private void Exit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }
}