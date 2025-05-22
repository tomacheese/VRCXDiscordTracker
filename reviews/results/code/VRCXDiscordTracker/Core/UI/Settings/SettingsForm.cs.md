# コードレビュー: VRCXDiscordTracker/Core/UI/Settings/SettingsForm.cs

## 概要

このファイルは、アプリケーションの設定を構成するためのWinFormsフォームを実装しています。データベースパス、Discord Webhook URL、通知設定などのユーザー設定を管理します。

## 良い点

- 設定の保存と読み込みが適切に実装されています
- 変更された設定を検出し、保存確認のダイアログを表示する機能が実装されています
- 必須項目（Discord Webhook URL）のバリデーションがあります
- 例外処理が適切に実装されています

## 改善点

### 1. デフォルト値の扱い

```csharp
// 現在のコード
private bool _lastSavedNotifyOnStart = true;
private bool _lastSavedNotifyOnExit = true;

// 改善案
// デフォルト値は一箇所で管理するべき
private bool _lastSavedNotifyOnStart = AppConfig.DefaultNotifyOnStart;
private bool _lastSavedNotifyOnExit = AppConfig.DefaultNotifyOnExit;
```

### 2. URLバリデーション

```csharp
// 現在のコード - 空チェックのみ
var textBoxDiscordWebhookUrlText = textBoxDiscordWebhookUrl.Text.Trim();
if (string.IsNullOrEmpty(textBoxDiscordWebhookUrlText))
{
    MessageBox.Show("Discord Webhook URL is required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

// 改善案 - より詳細なバリデーション
var webhookUrl = textBoxDiscordWebhookUrl.Text.Trim();
if (string.IsNullOrEmpty(webhookUrl))
{
    MessageBox.Show("Discord Webhook URLは必須項目です。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

// URLフォーマットの検証
if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out Uri? uriResult) ||
    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
{
    MessageBox.Show("有効なURLを入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

// Discord Webhook URLの基本パターン確認
if (!webhookUrl.Contains("discord.com/api/webhooks/"))
{
    var result = MessageBox.Show(
        "入力されたURLはDiscord WebhookのURLではないようです。続行しますか？", 
        "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        
    if (result != DialogResult.Yes)
    {
        return;
    }
}
```

### 3. データベースファイル選択機能

```csharp
// ファイル選択ダイアログを追加すべき
private void OnBrowseButtonClicked(object sender, EventArgs e)
{
    using var openFileDialog = new OpenFileDialog
    {
        Filter = "SQLiteデータベース (*.sqlite3)|*.sqlite3|すべてのファイル (*.*)|*.*",
        Title = "VRCXのデータベースファイルを選択",
        InitialDirectory = Path.GetDirectoryName(AppConstants.VRCXDefaultDatabasePath)
    };

    if (openFileDialog.ShowDialog() == DialogResult.OK)
    {
        textBoxDatabasePath.Text = openFileDialog.FileName;
    }
}
```

### 4. フォームの初期化と終了処理の分離

```csharp
// 現在のコード
private void OnFormClosing(object sender, FormClosingEventArgs e)
{
    var textBoxDiscordWebhookUrlText = textBoxDiscordWebhookUrl.Text.Trim();
    if (string.IsNullOrEmpty(textBoxDiscordWebhookUrlText))
    {
        Application.Exit(); // メッセージ表示後にExitを呼び出すと2回メッセージが表示されてしまうので先に呼び出す
        MessageBox.Show("Discord Webhook URL is required. Application will be closed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }
    // ...残りのコード
}

// 改善案
private void OnFormClosing(object sender, FormClosingEventArgs e)
{
    // 必須項目のチェックはフォームを閉じるかどうかの判断だけで、
    // アプリケーション終了はトレイアイコンが担当すべき
    var textBoxDiscordWebhookUrlText = textBoxDiscordWebhookUrl.Text.Trim();
    if (string.IsNullOrEmpty(textBoxDiscordWebhookUrlText))
    {
        MessageBox.Show("Discord Webhook URLは必須項目です。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        e.Cancel = true;
        return;
    }
    
    // 設定変更の確認
    CheckSettingsChanges(e);
}

private void CheckSettingsChanges(FormClosingEventArgs e)
{
    var changed = _lastSavedDatabasePath != textBoxDatabasePath.Text.Trim() ||
                  _lastSavedDiscordWebhookUrl != textBoxDiscordWebhookUrl.Text.Trim() ||
                  _lastSavedNotifyOnStart != checkBoxNotifyOnStart.Checked ||
                  _lastSavedNotifyOnExit != checkBoxNotifyOnExit.Checked;
                  
    if (!changed)
    {
        return;
    }

    DialogResult result = MessageBox.Show(
        "変更された設定があります。保存しますか？", 
        "確認", 
        MessageBoxButtons.YesNoCancel, 
        MessageBoxIcon.Question);
        
    switch (result)
    {
        case DialogResult.Yes:
            var saved = Save();
            if (!saved)
            {
                e.Cancel = true; // 保存に失敗した場合は閉じない
            }
            break;
        case DialogResult.Cancel:
            e.Cancel = true;
            break;
    }
}
```

### 5. 詳細なエラー表示

```csharp
// 現在のコード
catch (Exception ex)
{
    MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return false;
}

// 改善案
catch (Exception ex)
{
    var errorMessage = new StringBuilder()
        .AppendLine("設定の保存中にエラーが発生しました:")
        .AppendLine()
        .AppendLine($"エラー: {ex.Message}")
        .AppendLine()
        .AppendLine("詳細エラー情報を表示しますか？");
        
    var result = MessageBox.Show(
        errorMessage.ToString(), 
        "エラー", 
        MessageBoxButtons.YesNo, 
        MessageBoxIcon.Error);
        
    if (result == DialogResult.Yes)
    {
        MessageBox.Show(
            ex.ToString(), 
            "詳細エラー情報", 
            MessageBoxButtons.OK, 
            MessageBoxIcon.Information);
    }
    
    return false;
}
```

### 6. 設定項目の追加

現在の設定項目は基本的なものに限られています。以下のような追加機能を検討すべきです：

```csharp
// ポーリング間隔の設定
private NumericUpDown numericUpDownPollInterval;
// ...
numericUpDownPollInterval.Value = AppConfig.PollIntervalSeconds;
// ...
AppConfig.PollIntervalSeconds = (int)numericUpDownPollInterval.Value;

// Discord通知のカスタマイズオプション
private CheckBox checkBoxShowUserAvatars;
// ...
checkBoxShowUserAvatars.Checked = AppConfig.ShowUserAvatars;
// ...
AppConfig.ShowUserAvatars = checkBoxShowUserAvatars.Checked;
```

## セキュリティ上の考慮事項

- Discord Webhook URLは機密情報であり、安全に保存する必要があります。現在のプレーンテキスト保存ではなく、Windowsの資格情報マネージャーやより安全な方法での保存を検討すべきです。

## まとめ

`SettingsForm.cs`は基本的な機能を備えていますが、UXの改善、バリデーションの強化、そして追加機能の実装によりさらに使いやすくなる可能性があります。特に、データベースファイル選択のUXと、Discord Webhook URLのバリデーション強化が推奨されます。
