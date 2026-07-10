# CLAUDE.md

## プロジェクト概要
- **目的**: VRCX の SQLite データベースを監視し、VRChat のアクティビティ（インスタンス移動やメンバーの出入り）を Discord Webhook で通知する Windows 常駐アプリ。
- **主な機能**: VRCX データベースの監視、インスタンス種別・リージョンの解析、Discord Embed による通知、Windows トースト通知、トレイアイコンと設定画面、GitHub Releases を用いた自動アップデート。
- **構成**: 本体 `VRCXDiscordTracker`（WinExe）とアップデータ `VRCXDiscordTracker.Updater`（自己完結型 win-x64 実行ファイル）の 2 プロジェクト。`VRCXDiscordTracker.sln` で束ねる。

## 技術スタック
- **言語 / ターゲット**: C# / .NET 9.0（`net9.0-windows10.0.17763.0`）、WinForms、`PublishSingleFile`、Nullable 有効。
- **主要パッケージ**: `Discord.Net.Webhook`、`Microsoft.Toolkit.Uwp.Notifications`、`System.Data.SQLite`。

## 開発コマンド
```bash
# 依存関係の復元
dotnet restore VRCXDiscordTracker.sln

# ビルド（Release）
dotnet build VRCXDiscordTracker.sln /p:Configuration=Release

# 発行（Publish プロファイル: bin/Publish/ へ自己完結 win-x64 出力）
dotnet publish VRCXDiscordTracker.sln -p:PublishProfile=Publish

# フォーマット確認（CI と同一。差分があると失敗する）
dotnet format VRCXDiscordTracker.sln --verify-no-changes --severity warn

# フォーマット修正
dotnet format VRCXDiscordTracker.sln
```

## アーキテクチャと主要ファイル
- `VRCXDiscordTracker/Program.cs`: エントリポイント。
- `VRCXDiscordTracker/Core/`: コアロジック。
    - `VRCX/`: VRCX データベースへの接続とクエリ（`VRCXDatabase.cs`、埋め込み SQL `Queries/instanceMembers.sql`・`myLocations.sql`）。
    - `VRChat/`: インスタンス・場所の解析（`LocationParser.cs`、`InstanceType.cs`、`InstanceRegion.cs`）。
    - `Notification/`: Discord Webhook 通知（`DiscordNotificationService.cs`）と Windows トースト通知（`UwpNotificationService.cs`）。
    - `Config/`: `AppConfig.cs`（設定の読み書き）。
    - `Updater/`: `GitHubReleaseService.cs`、`UpdateChecker.cs` による自動アップデート。
    - `UI/`: `SettingsForm`、`TrayIcon`。
    - `AppConstants.cs`: VRCX デフォルト DB パス等の定数。
- `VRCXDiscordTracker.Updater/`: 本体を置き換えるアップデータ。

## 設定とデータの所在
- **アプリ設定**: 実行ディレクトリの `config.json`（`AppConfig`）。Discord Webhook URL 等を保持するため **コミットしない**。
- **VRCX DB のデフォルトパス**: `%APPDATA%\VRCX\VRCX.sqlite3`（`AppConstants.VRCXDefaultDatabasePath`）。設定で空欄にするとこの既定値が使われる。

## コーディング規約
- **命名**: C# 標準（クラス/メソッドは PascalCase、ローカル変数は camelCase）。
- **非同期**: `async/await` を用い、UI スレッドをブロックしない。DB 接続は使い終えたら閉じる。
- **設定アクセス**: 設定値は `AppConfig` 経由で取得する。
- **コメント**: 公開メソッド・インターフェースには日本語で XML ドキュメントコメントを付ける。
- **言語**: コメントは日本語、エラーメッセージは英語。日本語と英数字の間には半角スペースを入れる。
- **フォーマット**: `dotnet format` に従う（CI で `--verify-no-changes` により強制）。

## テスト
- 自動テストは未導入。変更後は実機（Windows）でのビルドと動作確認を行う。

## ドキュメント更新ルール
- ソース内の XML ドキュメントコメントを実装と一致させる。
- ディレクトリ構成・コマンド・依存パッケージを変更したら本ファイルの該当箇所も更新する。

## リポジトリ固有の注意
- Windows 専用（WinForms・UWP 通知）。API の可用性やスレッド挙動に留意する。
- VRCX の SQLite スキーマに依存するため、スキーマ変更時はクエリ（`Core/VRCX/Queries/`）の追従が必要。
- **会話言語**: 日本語。**コミット**: [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)（description は日本語）。**ブランチ**: [Conventional Branch](https://conventional-branch.github.io) 短縮形。
- Renovate が作成した PR には追加コミットしない。
