# プロジェクト詳細

## 概要

VRCXDiscordTrackerは、VRChat用のトラッキングツールであるVRCXのデータベースからユーザーの位置情報やインスタンス情報を読み取り、Discord Webhookを通じて通知するWindowsアプリケーションです。トレイに常駐し、設定された間隔でVRCXのSQLiteデータベースを監視し、ユーザーの移動情報やインスタンス内のメンバー情報を取得して、Discordに通知します。

## 技術スタック

### 言語・フレームワーク

- **プログラミング言語**: C# 9.0
- **フレームワーク**: .NET 9.0 (Windows)
- **UI**: Windows Forms
- **ターゲットOS**: Windows 10 (バージョン 10.0.17763.0 以上)

### 主要な依存パッケージ

#### メインアプリケーション (VRCXDiscordTracker)

- **Discord.Net.Webhook** (v3.17.4): Discordのウェブフック通知機能
- **Microsoft.Toolkit.Uwp.Notifications** (v7.1.3): Windows通知機能
- **System.Data.SQLite** (非明示的参照): SQLiteデータベース操作

#### アップデーターアプリケーション (VRCXDiscordTracker.Updater)

- **Newtonsoft.Json** (v13.0.3): JSON操作

### ビルド・パッケージング

- **PublishSingleFile**: true (単一ファイル実行形式として発行)
- **SelfContained**: true (依存関係を含めて独立して動作可能)
- **RuntimeIdentifier**: win-x64 (64bit Windows向け)

## プロジェクト構成

プロジェクトは2つのサブプロジェクトで構成されています：

1. **VRCXDiscordTracker**: メインアプリケーション
2. **VRCXDiscordTracker.Updater**: アップデートツール

### ディレクトリ構造

#### メインアプリケーション (VRCXDiscordTracker)

```
VRCXDiscordTracker/
├── Program.cs                              # エントリーポイント
├── Core/                                   # コア機能
│   ├── AppConstants.cs                     # アプリケーション定数
│   ├── VRCXDiscordTrackerController.cs     # メインコントローラー
│   ├── Config/                             # 設定関連
│   │   ├── AppConfig.cs                    # アプリ設定
│   │   └── ConfigData.cs                   # 設定データクラス
│   ├── Notification/                       # 通知機能
│   │   ├── DiscordEmbedMembers.cs          # Discord埋め込みメッセージ
│   │   ├── DiscordNotificationService.cs   # Discord通知サービス
│   │   └── UwpNotificationService.cs       # Windows通知サービス
│   ├── UI/                                 # ユーザーインターフェース
│   │   ├── Settings/                       # 設定画面
│   │   │   ├── SettingsForm.cs             # 設定フォーム
│   │   │   └── SettingsForm.Designer.cs    # 設定フォームデザイナー
│   │   └── TrayIcon/                       # システムトレイアイコン
│   │       └── TrayIcon.cs                 # トレイアイコン管理
│   ├── Updater/                            # アップデート機能
│   │   ├── GitHubReleaseService.cs         # GitHubリリース情報取得
│   │   ├── ReleaseInfo.cs                  # リリース情報モデル
│   │   ├── SemanticVersion.cs              # セマンティックバージョン管理
│   │   └── UpdateChecker.cs                # アップデートチェッカー
│   ├── VRChat/                             # VRChat関連
│   │   ├── InstanceRegion.cs               # インスタンス地域
│   │   ├── InstanceType.cs                 # インスタンスタイプ
│   │   ├── LocationParser.cs               # ロケーションID解析
│   │   └── VRChatInstance.cs               # VRChatインスタンス情報
│   └── VRCX/                               # VRCX関連
│       ├── InstanceMember.cs               # インスタンスメンバー
│       ├── MyLocation.cs                   # 自分のロケーション
│       ├── VRCXDatabase.cs                 # VRCXデータベース操作
│       └── Queries/                        # SQL
│           ├── instanceMembers.sql         # インスタンスメンバー取得SQL
│           └── myLocations.sql             # ロケーション取得SQL
├── Properties/                             # リソース
│   ├── Resources.Designer.cs               # リソースデザイナー
│   └── Resources.resx                      # リソースファイル
└── Resources/                              # アセット
    └── AppIcon.ico                         # アプリアイコン
```

#### アップデーターアプリケーション (VRCXDiscordTracker.Updater)

```
VRCXDiscordTracker.Updater/
├── Program.cs                              # エントリーポイント
└── Core/                                   # コア機能
    ├── AppConstants.cs                     # アプリケーション定数
    ├── GitHubReleaseService.cs             # GitHubリリース情報取得
    ├── ReleaseInfo.cs                      # リリース情報モデル
    ├── SemanticVersion.cs                  # セマンティックバージョン管理
    └── UpdaterHelper.cs                    # アップデートヘルパー
```

## 機能概要

1. **VRCXデータ監視**: VRCXのSQLiteデータベースを定期的に監視し、ユーザーの位置情報とインスタンスメンバー情報を取得
2. **Discord通知**: ユーザーの移動やインスタンス情報をDiscord Webhookを通じて通知
3. **Windows通知**: アプリケーション状態の変化をWindowsトースト通知で表示
4. **自動アップデート**: GitHubリリースからの最新バージョンの確認と自動アップデート
5. **システムトレイ統合**: バックグラウンドで動作し、システムトレイからアクセス可能
6. **設定管理**: データベースパス、Discord Webhook URL、通知設定などのユーザー設定

## ライセンス

MIT License

## CI/CD

Renovateを使用して依存関係の自動更新を行っています。

## テスト

専用のテストコードは現時点で存在しません。
