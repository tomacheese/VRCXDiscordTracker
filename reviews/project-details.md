# VRCXDiscordTracker プロジェクト詳細

## プロジェクト概要

VRCXDiscordTrackerは、VRCXのSQLiteデータベースを監視し、VRChatのインスタンス参加者情報をDiscordに通知するWindows向けシステムトレイアプリケーションです。

## 技術スタック

### 使用言語・フレームワーク

- **言語**: C# 
- **フレームワーク**: Windows Forms (WinForms)
- **.NETバージョン**: .NET 9.0
- **ターゲットOS**: Windows 10.0.17763.0以降
- **出力タイプ**: 
  - VRCXDiscordTracker: WinExe (Windowsアプリケーション)
  - VRCXDiscordTracker.Updater: Exe (コンソールアプリケーション)

### 依存パッケージ・ライブラリ

#### VRCXDiscordTracker (メインプロジェクト)

| パッケージ | バージョン | 用途 |
|-----------|----------|------|
| Discord.Net.Webhook | 3.17.4 | Discord Webhookの送信 |
| Microsoft.Toolkit.Uwp.Notifications | 7.1.3 | Windows通知 |
| System.Data.SQLite | 1.0.119 | SQLiteデータベース操作 |

#### VRCXDiscordTracker.Updater (アップデーター)

| パッケージ | バージョン | 用途 |
|-----------|----------|------|
| Newtonsoft.Json | 13.0.3 | JSON処理 |

## ディレクトリ構成

```
VRCXDiscordTracker/
├── VRCXDiscordTracker/          # メインアプリケーション
│   ├── Core/
│   │   ├── Config/              # 設定管理
│   │   │   ├── AppConfig.cs
│   │   │   └── ConfigData.cs
│   │   ├── Notification/        # Discord・Windows通知
│   │   │   ├── DiscordEmbedMembers.cs
│   │   │   ├── DiscordNotificationService.cs
│   │   │   └── UwpNotificationService.cs
│   │   ├── UI/                  # ユーザーインターフェース
│   │   │   ├── Settings/        # 設定画面
│   │   │   │   ├── SettingsForm.cs
│   │   │   │   ├── SettingsForm.Designer.cs
│   │   │   │   └── SettingsForm.resx
│   │   │   └── TrayIcon/        # システムトレイ
│   │   │       └── TrayIcon.cs
│   │   ├── Updater/             # アップデート機能
│   │   │   ├── GitHubReleaseService.cs
│   │   │   ├── ReleaseInfo.cs
│   │   │   ├── SemanticVersion.cs
│   │   │   └── UpdateChecker.cs
│   │   ├── VRCX/                # VRCXデータベース関連
│   │   │   ├── InstanceMember.cs
│   │   │   ├── MyLocation.cs
│   │   │   ├── VRCXDatabase.cs
│   │   │   └── Queries/         # SQLクエリファイル
│   │   │       ├── instanceMembers.sql
│   │   │       └── myLocations.sql
│   │   ├── VRChat/              # VRChat関連のモデル
│   │   │   ├── InstanceRegion.cs
│   │   │   ├── InstanceType.cs
│   │   │   ├── LocationParser.cs
│   │   │   └── VRChatInstance.cs
│   │   ├── AppConstants.cs
│   │   └── VRCXDiscordTrackerController.cs
│   ├── Program.cs
│   ├── Properties/
│   │   ├── PublishProfiles/
│   │   │   └── Publish.pubxml
│   │   ├── Resources.Designer.cs
│   │   ├── Resources.resx
│   │   └── launchSettings.json
│   └── Resources/
│       └── AppIcon.ico
└── VRCXDiscordTracker.Updater/  # アップデーターツール
    ├── Core/
    │   ├── AppConstants.cs
    │   ├── GitHubReleaseService.cs
    │   ├── ReleaseInfo.cs
    │   ├── SemanticVersion.cs
    │   └── UpdaterHelper.cs
    ├── Program.cs
    └── Properties/
        └── PublishProfiles/
            └── Publish.pubxml
```

## 主な機能

1. **VRCXデータベース監視**
   - SQLiteデータベースをポーリング
   - インスタンス参加者の変更を検知

2. **通知機能**
   - Discord Webhook経由での通知
   - Windows UWP通知

3. **システムトレイ常駐**
   - バックグラウンドで動作
   - 設定画面へのアクセス

4. **自動アップデート**
   - GitHub Releasesからの更新確認
   - セマンティックバージョニング対応

## ビルド・パブリッシュ設定

- **PublishSingleFile**: true (単一実行ファイル生成)
- **SelfContained**: false (共有フレームワーク使用)
- **RuntimeIdentifier**: win-x64
- **EnableCompressionInSingleFile**: true

## CI/CD

### GitHub Actions ワークフロー

1. **ci.yml** - ビルド・コードスタイルチェック
   - トリガー: master/mainブランチへのpush、PR
   - .NET 9.0でビルド・パブリッシュ
   - `dotnet format`によるコードスタイルチェック

2. **release.yml** - リリース自動化
   - semantic versioningによる自動バージョニング
   - GitHubリリースの作成とZIPファイルのアップロード

3. **review.yml** - PRレビュー管理
   - 自動レビュアー割り当て

## 依存関係管理

- **renovate.json**: 外部テンプレート（book000/templates）の設定を継承
- 依存関係の自動更新が設定済み

## テストコード

- テストプロジェクトは存在しない

## ライセンス

- MIT License

## パッケージマネージャ

- NuGet (PackageReference形式)

## その他の特徴

- 埋め込みSQLクエリファイル使用
- Windows専用アプリケーション
- VRCXとの連携に特化