# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/UI/Settings/SettingsForm.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 139行（大規模）

## 概要
WinForms設定画面のコードビハインド。アプリケーション設定の表示・保存・検証機能を実装している。

## 総合評価
**スコア: 5/10**

基本機能は動作するが、設計、エラーハンドリング、結合度の観点で重要な改善が必要。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐☆☆☆
**Issues:**
- Programクラスへの強い結合
- 設定とコントローラの直接操作
- UI処理とビジネスロジックの混在
- グローバル状態への依存

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐☆
**Good:**
- C#命名規約に準拠
- XMLドキュメンテーションが適切

**Issues:**
- 長いメソッド
- 複雑な条件分岐

### 3. セキュリティ上の問題 ⭐⭐⭐☆☆
**Good:**
- 基本的な入力検証

**Issues:**
- URL検証の不完全性
- ファイルパス検証不足

### 4. パフォーマンスの問題 ⭐⭐⭐☆☆
**Good:**
- 適切なUIコントロール使用

**Issues:**
- 不要なコントローラの再作成
- UI応答性の問題

### 5. 可読性・保守性 ⭐⭐⭐☆☆
**Good:**
- 明確なメソッド名

**Issues:**
- 複雑な条件式
- 状態管理の複雑さ

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- UIとロジックの強い結合
- 外部依存への直接アクセス
- Programクラスへの依存

## 具体的な問題点と改善提案

### 1. 【重要度：高】MVP/MVVMパターンの導入
**問題**: UIロジックとビジネスロジックの混在、テスト困難

**改善案**:
```csharp
/// <summary>
/// 設定画面のプレゼンター
/// </summary>
internal class SettingsPresenter
{
    private readonly ISettingsView _view;
    private readonly IAppConfigService _configService;
    private readonly ITrackerService _trackerService;
    private readonly INotificationService _notificationService;

    public SettingsPresenter(
        ISettingsView view,
        IAppConfigService configService,
        ITrackerService trackerService,
        INotificationService notificationService)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _trackerService = trackerService ?? throw new ArgumentNullException(nameof(trackerService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        _view.LoadRequested += OnLoadRequested;
        _view.SaveRequested += OnSaveRequested;
        _view.FormClosing += OnFormClosing;
    }

    private async void OnLoadRequested(object sender, EventArgs e)
    {
        try
        {
            var config = await _configService.LoadConfigAsync();
            var settings = new SettingsViewModel
            {
                DatabasePath = config.DatabasePath,
                DiscordWebhookUrl = config.DiscordWebhookUrl,
                NotifyOnStart = config.NotifyOnStart,
                NotifyOnExit = config.NotifyOnExit,
                LocationCount = config.LocationCount
            };

            _view.DisplaySettings(settings);
        }
        catch (Exception ex)
        {
            _view.ShowError($"Failed to load settings: {ex.Message}");
        }
    }

    private async void OnSaveRequested(object sender, SaveRequestedEventArgs e)
    {
        try
        {
            var validationResult = ValidateSettings(e.Settings);
            if (!validationResult.IsValid)
            {
                _view.ShowValidationErrors(validationResult.Errors);
                return;
            }

            await _configService.SaveConfigAsync(e.Settings.ToConfigData());
            await _trackerService.RestartAsync(e.Settings.DatabasePath);
            
            await _notificationService.ShowNotificationAsync("Settings Saved", "Settings have been saved successfully.");
            
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _view.ShowError($"Failed to save settings: {ex.Message}");
            e.Cancel = true;
        }
    }

    private ValidationResult ValidateSettings(SettingsViewModel settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
        {
            errors.Add("Discord Webhook URL is required.");
        }
        else if (!Uri.TryCreate(settings.DiscordWebhookUrl, UriKind.Absolute, out var uri) || 
                 (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("Discord Webhook URL must be a valid HTTP/HTTPS URL.");
        }

        if (!string.IsNullOrWhiteSpace(settings.DatabasePath) && !File.Exists(settings.DatabasePath))
        {
            errors.Add("Specified database file does not exist.");
        }

        if (settings.LocationCount < 1 || settings.LocationCount > 100)
        {
            errors.Add("Location count must be between 1 and 100.");
        }

        return new ValidationResult(errors);
    }
}

/// <summary>
/// 設定画面のインターフェース
/// </summary>
public interface ISettingsView
{
    event EventHandler LoadRequested;
    event EventHandler<SaveRequestedEventArgs> SaveRequested;
    event EventHandler<FormClosingEventArgs> FormClosing;

    void DisplaySettings(SettingsViewModel settings);
    void ShowError(string message);
    void ShowValidationErrors(IEnumerable<string> errors);
    void Close();
}

/// <summary>
/// 設定ビューモデル
/// </summary>
public class SettingsViewModel
{
    public string DatabasePath { get; set; } = string.Empty;
    public string DiscordWebhookUrl { get; set; } = string.Empty;
    public bool NotifyOnStart { get; set; }
    public bool NotifyOnExit { get; set; }
    public int LocationCount { get; set; }

    public ConfigData ToConfigData() => new()
    {
        DatabasePath = DatabasePath,
        DiscordWebhookUrl = DiscordWebhookUrl,
        NotifyOnStart = NotifyOnStart,
        NotifyOnExit = NotifyOnExit,
        LocationCount = LocationCount
    };
}

/// <summary>
/// 保存要求イベント引数
/// </summary>
public class SaveRequestedEventArgs : EventArgs
{
    public SettingsViewModel Settings { get; }
    public bool Handled { get; set; }
    public bool Cancel { get; set; }

    public SaveRequestedEventArgs(SettingsViewModel settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}
```

### 2. 【重要度：高】改善されたフォーム実装
**改善案**:
```csharp
public partial class SettingsForm : Form, ISettingsView
{
    private SettingsPresenter? _presenter;
    private SettingsViewModel _originalSettings = new();
    private SettingsViewModel _currentSettings = new();

    public event EventHandler? LoadRequested;
    public event EventHandler<SaveRequestedEventArgs>? SaveRequested;

    public SettingsForm()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    public void SetPresenter(SettingsPresenter presenter)
    {
        _presenter = presenter;
    }

    public void DisplaySettings(SettingsViewModel settings)
    {
        _originalSettings = settings;
        _currentSettings = settings.Clone();

        textBoxDatabasePath.Text = settings.DatabasePath;
        textBoxDiscordWebhookUrl.Text = settings.DiscordWebhookUrl;
        checkBoxNotifyOnStart.Checked = settings.NotifyOnStart;
        checkBoxNotifyOnExit.Checked = settings.NotifyOnExit;
        numericUpDownLocationCount.Value = settings.LocationCount;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public void ShowValidationErrors(IEnumerable<string> errors)
    {
        var message = string.Join(Environment.NewLine, errors);
        ShowError(message);
    }

    private void SetupEventHandlers()
    {
        Load += (sender, e) => LoadRequested?.Invoke(this, EventArgs.Empty);
        
        textBoxDatabasePath.TextChanged += OnSettingChanged;
        textBoxDiscordWebhookUrl.TextChanged += OnSettingChanged;
        checkBoxNotifyOnStart.CheckedChanged += OnSettingChanged;
        checkBoxNotifyOnExit.CheckedChanged += OnSettingChanged;
        numericUpDownLocationCount.ValueChanged += OnSettingChanged;
        
        buttonSave.Click += OnSaveClicked;
        buttonCancel.Click += (sender, e) => Close();
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        UpdateCurrentSettings();
        UpdateUIState();
    }

    private void UpdateCurrentSettings()
    {
        _currentSettings = new SettingsViewModel
        {
            DatabasePath = textBoxDatabasePath.Text.Trim(),
            DiscordWebhookUrl = textBoxDiscordWebhookUrl.Text.Trim(),
            NotifyOnStart = checkBoxNotifyOnStart.Checked,
            NotifyOnExit = checkBoxNotifyOnExit.Checked,
            LocationCount = (int)numericUpDownLocationCount.Value
        };
    }

    private void UpdateUIState()
    {
        var hasChanges = !_originalSettings.Equals(_currentSettings);
        buttonSave.Enabled = hasChanges;
        
        // リアルタイム検証
        var hasValidUrl = !string.IsNullOrWhiteSpace(_currentSettings.DiscordWebhookUrl) &&
                         Uri.TryCreate(_currentSettings.DiscordWebhookUrl, UriKind.Absolute, out _);
        
        labelUrlStatus.Text = hasValidUrl ? "✓ Valid URL" : "⚠ Invalid URL";
        labelUrlStatus.ForeColor = hasValidUrl ? Color.Green : Color.Red;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var args = new SaveRequestedEventArgs(_currentSettings);
        SaveRequested?.Invoke(this, args);
        
        if (args.Handled && !args.Cancel)
        {
            _originalSettings = _currentSettings.Clone();
            UpdateUIState();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_originalSettings.Equals(_currentSettings))
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save them?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            switch (result)
            {
                case DialogResult.Yes:
                    var args = new SaveRequestedEventArgs(_currentSettings);
                    SaveRequested?.Invoke(this, args);
                    e.Cancel = args.Cancel;
                    break;
                case DialogResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        base.OnFormClosing(e);
    }
}
```

### 3. 【重要度：中】非同期処理の改善
**改善案**:
```csharp
/// <summary>
/// 非同期設定操作
/// </summary>
internal class AsyncSettingsOperations
{
    private readonly IAppConfigService _configService;
    private readonly ITrackerService _trackerService;

    public AsyncSettingsOperations(IAppConfigService configService, ITrackerService trackerService)
    {
        _configService = configService;
        _trackerService = trackerService;
    }

    public async Task<OperationResult> SaveSettingsAsync(SettingsViewModel settings, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            // 設定保存
            await _configService.SaveConfigAsync(settings.ToConfigData(), cancellationToken);
            
            // トラッカー再起動
            await _trackerService.RestartAsync(settings.DatabasePath, cancellationToken);
            
            scope.Complete();
            
            return OperationResult.Success("Settings saved successfully");
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Cancelled();
        }
        catch (Exception ex)
        {
            return OperationResult.Error($"Failed to save settings: {ex.Message}");
        }
    }

    public async Task<OperationResult<SettingsViewModel>> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _configService.LoadConfigAsync(cancellationToken);
            var viewModel = SettingsViewModel.FromConfigData(config);
            
            return OperationResult<SettingsViewModel>.Success(viewModel);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<SettingsViewModel>.Cancelled();
        }
        catch (Exception ex)
        {
            return OperationResult<SettingsViewModel>.Error($"Failed to load settings: {ex.Message}");
        }
    }
}

/// <summary>
/// 操作結果を表すクラス
/// </summary>
public class OperationResult
{
    public bool IsSuccess { get; }
    public bool IsCancelled { get; }
    public string? ErrorMessage { get; }
    public string? SuccessMessage { get; }

    protected OperationResult(bool isSuccess, bool isCancelled, string? errorMessage, string? successMessage)
    {
        IsSuccess = isSuccess;
        IsCancelled = isCancelled;
        ErrorMessage = errorMessage;
        SuccessMessage = successMessage;
    }

    public static OperationResult Success(string? message = null) => new(true, false, null, message);
    public static OperationResult Error(string errorMessage) => new(false, false, errorMessage, null);
    public static OperationResult Cancelled() => new(false, true, null, null);
}

public class OperationResult<T> : OperationResult
{
    public T? Value { get; }

    private OperationResult(bool isSuccess, bool isCancelled, string? errorMessage, string? successMessage, T? value)
        : base(isSuccess, isCancelled, errorMessage, successMessage)
    {
        Value = value;
    }

    public static OperationResult<T> Success(T value, string? message = null) => new(true, false, null, message, value);
    public static new OperationResult<T> Error(string errorMessage) => new(false, false, errorMessage, null, default);
    public static new OperationResult<T> Cancelled() => new(false, true, null, null, default);
}
```

### 4. 【重要度：低】ユーザビリティの向上
**改善案**:
```csharp
/// <summary>
/// 設定フォームのUX改善
/// </summary>
public partial class EnhancedSettingsForm : Form
{
    private readonly ToolTip _toolTip = new();
    private readonly ErrorProvider _errorProvider = new();

    private void SetupUXEnhancements()
    {
        // ツールチップ設定
        _toolTip.SetToolTip(textBoxDatabasePath, "Path to VRCX SQLite database file");
        _toolTip.SetToolTip(textBoxDiscordWebhookUrl, "Discord webhook URL for notifications");
        _toolTip.SetToolTip(checkBoxNotifyOnStart, "Show notification when application starts");
        
        // ファイルドロップ対応
        textBoxDatabasePath.AllowDrop = true;
        textBoxDatabasePath.DragEnter += OnDatabasePathDragEnter;
        textBoxDatabasePath.DragDrop += OnDatabasePathDragDrop;
        
        // ブラウズボタン
        buttonBrowseDatabasePath.Click += OnBrowseDatabasePath;
        
        // リアルタイム検証
        textBoxDiscordWebhookUrl.TextChanged += OnWebhookUrlChanged;
    }

    private void OnDatabasePathDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            var isValidFile = files?.Length == 1 && 
                             Path.GetExtension(files[0]).Equals(".sqlite3", StringComparison.OrdinalIgnoreCase);
            
            e.Effect = isValidFile ? DragDropEffects.Copy : DragDropEffects.None;
        }
    }

    private void OnDatabasePathDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length == 1)
        {
            textBoxDatabasePath.Text = files[0];
        }
    }

    private void OnBrowseDatabasePath(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select VRCX Database File",
            Filter = "SQLite Database Files (*.sqlite3)|*.sqlite3|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            textBoxDatabasePath.Text = dialog.FileName;
        }
    }

    private async void OnWebhookUrlChanged(object? sender, EventArgs e)
    {
        var url = textBoxDiscordWebhookUrl.Text.Trim();
        
        if (string.IsNullOrEmpty(url))
        {
            _errorProvider.SetError(textBoxDiscordWebhookUrl, "");
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            _errorProvider.SetError(textBoxDiscordWebhookUrl, "Invalid URL format");
            return;
        }

        // 非同期でWebhook接続テスト（オプション）
        if (checkBoxTestWebhook.Checked)
        {
            await TestWebhookAsync(url);
        }
    }

    private async Task TestWebhookAsync(string webhookUrl)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // Webhook接続テストの実装
            _errorProvider.SetError(textBoxDiscordWebhookUrl, "");
            labelWebhookStatus.Text = "✓ Webhook reachable";
            labelWebhookStatus.ForeColor = Color.Green;
        }
        catch
        {
            labelWebhookStatus.Text = "⚠ Webhook test failed";
            labelWebhookStatus.ForeColor = Color.Orange;
        }
    }
}
```

## 推奨されるNext Steps
1. MVP/MVVMパターンの導入（高優先度）
2. 依存関係注入の実装（高優先度）
3. 非同期処理の改善（中優先度）
4. UX強化の実装（低優先度）
5. 包括的な単体テストの追加（中優先度）

## コメント
基本的なWinForms実装としては動作しますが、現代的なアプリケーション設計の観点で多くの改善が必要です。特にProgramクラスへの直接依存とUIロジックの混在は、テスト容易性と保守性を大きく損なっています。MVP/MVVMパターンの導入により、より堅牢で保守しやすいUIアーキテクチャにする必要があります。