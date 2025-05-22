# コード・プログラムファイル一覧

## メインアプリケーション (VRCXDiscordTracker)

### コアファイル

- `VRCXDiscordTracker/Program.cs` - エントリポイント
- `VRCXDiscordTracker/Core/AppConstants.cs` - アプリケーション定数
- `VRCXDiscordTracker/Core/VRCXDiscordTrackerController.cs` - コントローラークラス

### 構成関連

- `VRCXDiscordTracker/Core/Config/AppConfig.cs` - アプリケーション設定
- `VRCXDiscordTracker/Core/Config/ConfigData.cs` - 設定データモデル

### VRChat/VRCX連携

- `VRCXDiscordTracker/Core/VRCX/VRCXDatabase.cs` - VRCXデータベースアクセス
- `VRCXDiscordTracker/Core/VRCX/MyLocation.cs` - 位置情報クラス
- `VRCXDiscordTracker/Core/VRCX/InstanceMember.cs` - インスタンスメンバークラス
- `VRCXDiscordTracker/Core/VRChat/VRChatInstance.cs` - VRChatインスタンスクラス
- `VRCXDiscordTracker/Core/VRChat/LocationParser.cs` - 位置情報パーサー
- `VRCXDiscordTracker/Core/VRChat/InstanceType.cs` - インスタンスタイプ
- `VRCXDiscordTracker/Core/VRChat/InstanceRegion.cs` - インスタンスリージョン

### 通知関連

- `VRCXDiscordTracker/Core/Notification/DiscordEmbedMembers.cs` - Discord Embed生成
- `VRCXDiscordTracker/Core/Notification/DiscordNotificationService.cs` - Discord通知サービス
- `VRCXDiscordTracker/Core/Notification/UwpNotificationService.cs` - UWP通知サービス

### UI関連

- `VRCXDiscordTracker/Core/UI/Settings/SettingsForm.cs` - 設定フォーム
- `VRCXDiscordTracker/Core/UI/Settings/SettingsForm.Designer.cs` - 設定フォームデザイナー
- `VRCXDiscordTracker/Core/UI/TrayIcon/TrayIcon.cs` - トレイアイコン

### アップデート関連

- `VRCXDiscordTracker/Core/Updater/UpdateChecker.cs` - アップデートチェッカー
- `VRCXDiscordTracker/Core/Updater/GitHubReleaseService.cs` - GitHubリリースサービス
- `VRCXDiscordTracker/Core/Updater/ReleaseInfo.cs` - リリース情報クラス
- `VRCXDiscordTracker/Core/Updater/SemanticVersion.cs` - セマンティックバージョン

## アップデーターアプリケーション (VRCXDiscordTracker.Updater)

- `VRCXDiscordTracker.Updater/Program.cs` - エントリポイント
- `VRCXDiscordTracker.Updater/Core/AppConstants.cs` - アプリケーション定数
- `VRCXDiscordTracker.Updater/Core/GitHubReleaseService.cs` - GitHubリリースサービス
- `VRCXDiscordTracker.Updater/Core/ReleaseInfo.cs` - リリース情報クラス
- `VRCXDiscordTracker.Updater/Core/SemanticVersion.cs` - セマンティックバージョン
- `VRCXDiscordTracker.Updater/Core/UpdaterHelper.cs` - アップデートヘルパー

## SQLクエリファイル

- `VRCXDiscordTracker/Core/VRCX/Queries/instanceMembers.sql` - インスタンスメンバークエリ
- `VRCXDiscordTracker/Core/VRCX/Queries/myLocations.sql` - ロケーション情報クエリ
