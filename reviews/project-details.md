# VRCXDiscordTracker プロジェクト詳細

## 基本情報

- **言語**: C# (.NET)
- **フレームワーク**: .NET 9.0 Windows (Windows 10.0.17763.0)
- **アプリケーション種別**: Windows Forms アプリケーション + コンソールアプリケーション (Updater)
- **UI技術**: Windows Forms、UWP通知、システムトレイアイコン
- **リポジトリ**: GitHub (tomacheese/VRCXDiscordTracker)
- **ライセンス**: リポジトリにLICENSEファイルあり（詳細は要確認）

## 依存パッケージ・ライブラリ

### メインアプリケーション (VRCXDiscordTracker)

- **Discord.Net.Webhook** (v3.17.4): Discord Webhookを使用して通知を送信
- **Microsoft.Toolkit.Uwp.Notifications** (v7.1.3): Windows 10/11のUWP通知機能を使用
- **System.Data.SQLite** (v1.0.119): SQLiteデータベースにアクセスするためのライブラリ

### アップデーター (VRCXDiscordTracker.Updater)

- **Newtonsoft.Json** (v13.0.3): JSONデータの処理

## パッケージマネージャ

- **NuGet**: .NETプロジェクトの標準パッケージマネージャー

## ディレクトリ構成

```
VRCXDiscordTracker/
├── VRCXDiscordTracker/            # メインアプリケーション
│   ├── Program.cs                 # エントリーポイント
│   ├── Resources/                 # リソースファイル（アイコンなど）
│   ├── Properties/                # プロジェクト設定
│   ├── Core/                      # コアクラスライブラリ
│   │   ├── AppConstants.cs        # アプリの定数定義
│   │   ├── Config/                # 設定関連
│   │   ├── Notification/          # 通知機能
│   │   ├── UI/                    # UI関連コード
│   │   │   ├── Settings/          # 設定画面
│   │   │   └── TrayIcon/          # システムトレイアイコン
│   │   ├── Updater/               # アップデート機能
│   │   ├── VRChat/                # VRChat関連
│   │   └── VRCX/                  # VRCXデータベース連携
│   └── VRCXDiscordTracker.csproj  # プロジェクトファイル
└── VRCXDiscordTracker.Updater/    # アップデーターアプリ
    ├── Program.cs                 # エントリーポイント
    ├── Core/                      # コアクラスライブラリ
    │   ├── AppConstants.cs        # アプリの定数定義
    │   ├── GitHubReleaseService.cs # GitHub APIを使用したリリース情報取得
    │   ├── ReleaseInfo.cs         # リリース情報クラス
    │   ├── SemanticVersion.cs     # セマンティックバージョン管理
    │   └── UpdaterHelper.cs       # アップデートヘルパー
    └── VRCXDiscordTracker.Updater.csproj # プロジェクトファイル
```

## プロジェクト概要

VRCXDiscordTrackerは、VRChatのクライアントツールである「VRCX」からユーザーの活動データを取得し、Discord WebhookでDiscordサーバーに通知するツールです。

### 主な機能

1. **VRCXデータベース連携**: VRCXのSQLiteデータベースから位置情報やインスタンス情報を取得
2. **Discord通知**: DiscordのWebhookを使用して、VRChatでのユーザーの移動や友達との接続を通知
3. **UWP通知**: Windows 10/11のトースト通知でアプリの状態を表示
4. **自動アップデート**: GitHubのリリースから最新バージョンを取得して自動アップデート
5. **システムトレイ操作**: システムトレイからアプリの設定や終了を操作

### VRCXDiscordTracker (メインアプリ)

- Windows Formsをベースとしたシステムトレイアプリケーション
- VRCXのSQLiteデータベースを定期的に監視し、ユーザーのワールド移動やフレンドとの接続状態を検出
- DiscordのWebhookを使用して、指定されたチャンネルに通知を送信
- セットアップ画面で基本設定を構成可能

### VRCXDiscordTracker.Updater (アップデーターアプリ)

- GitHubのリリースから最新バージョンを取得して自動更新を実行
- メインアプリからの指示で動作し、プロセスを終了してから更新を適用

## テストコード

このプロジェクトには明示的なテストコードが含まれていません。テスト自動化のためのフレームワーク（xUnit、NUnit、MSTestなど）は導入されていません。手動テストに依存していると思われます。

## ビルド・発行設定

- **PublishSingleFile**: 単一の実行ファイルとして発行
- **SelfContained**: アップデーターはSelf-Containedとして発行（依存関係を同梱）
- **RuntimeIdentifier**: win-x64
- **デバッグタイプ**: embedded（デバッグ情報を実行ファイルに埋め込み）

## CI/CD

- **Renovate**: `renovate.json`ファイルの存在から、依存パッケージの自動更新が設定されています
- 他のCI/CD設定（GitHub Actionsなど）が存在するかは不明です

## セキュリティ特性

- **データアクセス**: VRCXのSQLiteデータベースへの読み取りのみ（書き込みなし）
- **Web API**: Discord Webhookでの外部通信
- **エラーハンドリング**: 例外発生時にGitHub Issuesへの誘導
