# プログラムコード レビュー対象ファイル

## メインアプリケーション (VRCXDiscordTracker)

### ルート

- `/VRCXDiscordTracker/Program.cs` - メインエントリーポイント
- `/VRCXDiscordTracker/VRCXDiscordTracker.csproj` - プロジェクトファイル

### コア機能

- `/VRCXDiscordTracker/Core/AppConstants.cs` - アプリケーション定数
- `/VRCXDiscordTracker/Core/VRCXDiscordTrackerController.cs` - メインコントローラー

### 設定関連

- `/VRCXDiscordTracker/Core/Config/AppConfig.cs` - アプリケーション設定
- `/VRCXDiscordTracker/Core/Config/ConfigData.cs` - 設定データモデル

### 通知機能

- `/VRCXDiscordTracker/Core/Notification/DiscordEmbedMembers.cs` - Discord埋め込みメッセージ
- `/VRCXDiscordTracker/Core/Notification/DiscordNotificationService.cs` - Discord通知サービス
- `/VRCXDiscordTracker/Core/Notification/UwpNotificationService.cs` - Windows通知サービス

### UI

- `/VRCXDiscordTracker/Core/UI/Settings/SettingsForm.cs` - 設定フォーム
- `/VRCXDiscordTracker/Core/UI/Settings/SettingsForm.Designer.cs` - 設定フォームデザイナー
- `/VRCXDiscordTracker/Core/UI/TrayIcon/TrayIcon.cs` - トレイアイコン管理

### アップデート機能

- `/VRCXDiscordTracker/Core/Updater/GitHubReleaseService.cs` - GitHubリリース情報取得
- `/VRCXDiscordTracker/Core/Updater/ReleaseInfo.cs` - リリース情報モデル
- `/VRCXDiscordTracker/Core/Updater/SemanticVersion.cs` - セマンティックバージョン管理
- `/VRCXDiscordTracker/Core/Updater/UpdateChecker.cs` - アップデートチェッカー

### VRChat関連

- `/VRCXDiscordTracker/Core/VRChat/InstanceRegion.cs` - インスタンス地域
- `/VRCXDiscordTracker/Core/VRChat/InstanceType.cs` - インスタンスタイプ
- `/VRCXDiscordTracker/Core/VRChat/LocationParser.cs` - ロケーションID解析
- `/VRCXDiscordTracker/Core/VRChat/VRChatInstance.cs` - VRChatインスタンス情報

### VRCX関連

- `/VRCXDiscordTracker/Core/VRCX/InstanceMember.cs` - インスタンスメンバー
- `/VRCXDiscordTracker/Core/VRCX/MyLocation.cs` - 自分のロケーション
- `/VRCXDiscordTracker/Core/VRCX/VRCXDatabase.cs` - VRCXデータベース操作
- `/VRCXDiscordTracker/Core/VRCX/Queries/instanceMembers.sql` - インスタンスメンバー取得SQL
- `/VRCXDiscordTracker/Core/VRCX/Queries/myLocations.sql` - ロケーション取得SQL

### リソース

- `/VRCXDiscordTracker/Properties/Resources.Designer.cs` - リソースデザイナー

## アップデーターアプリケーション (VRCXDiscordTracker.Updater)

### ルート

- `/VRCXDiscordTracker.Updater/Program.cs` - メインエントリーポイント
- `/VRCXDiscordTracker.Updater/VRCXDiscordTracker.Updater.csproj` - プロジェクトファイル

### コア機能

- `/VRCXDiscordTracker.Updater/Core/AppConstants.cs` - アプリケーション定数
- `/VRCXDiscordTracker.Updater/Core/GitHubReleaseService.cs` - GitHubリリース情報取得
- `/VRCXDiscordTracker.Updater/Core/ReleaseInfo.cs` - リリース情報モデル
- `/VRCXDiscordTracker.Updater/Core/SemanticVersion.cs` - セマンティックバージョン管理
- `/VRCXDiscordTracker.Updater/Core/UpdaterHelper.cs` - アップデートヘルパー
