# GitHub Copilot Instructions

## プロジェクト概要
- **目的**: VRCX のデータベースを監視し、VRChat のアクティビティ（インスタンス移動など）を Discord Webhook を介して通知するツール。
- **主な機能**:
    - VRCX の SQLite データベースの監視とデータ取得。
    - インスタンス情報の解析と Discord Embed 形式での通知。
    - Windows トレイアイコンによるバックグラウンド動作と設定管理。
    - GitHub Releases を利用した自動アップデート機能。
    - Windows トースト通知によるアプリ状態の通知。
- **対象ユーザー**: VRCX を利用している VRChat ユーザー。

## 共通ルール
- 会話は日本語で行う。
- PR とコミットは [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) に従う。
    - `<type>(<scope>): <description>` 形式。
    - `<description>` は日本語で記載する。
- 日本語と英数字の間には半角スペースを入れる。

## 技術スタック
- **言語**: C# (.NET 9.0)
- **UI フレームワーク**: Windows Forms (WinForms)
- **データベース**: SQLite (System.Data.SQLite)
- **通知**: Discord.Net.Webhook, Microsoft.Toolkit.Uwp.Notifications

## 開発コマンド
```bash
# 依存関係の復元
dotnet restore VRCXDiscordTracker.sln

# ビルド (Debug)
dotnet build VRCXDiscordTracker.sln

# ビルド (Release)
dotnet build VRCXDiscordTracker.sln /p:Configuration=Release

# 発行
dotnet publish VRCXDiscordTracker.sln -p:PublishProfile=Publish

# コードフォーマットの確認
dotnet format VRCXDiscordTracker.sln --verify-no-changes --severity warn

# コードフォーマットの修正
dotnet format VRCXDiscordTracker.sln
```

## コーディング規約
- **命名規則**: C# の標準的な命名規則（PascalCase for classes/methods, camelCase for local variables）に従う。
- **ドキュメント**: 公開メソッドやインターフェースには XML ドキュメントコメントを日本語で記載する。
- **非同期プログラミング**: `async/await` を適切に使用し、UI スレッドをブロックしないようにする。

## テスト方針
- 現時点ではテストプロジェクトは存在しない。新規機能追加時には、必要に応じてテストプロジェクトの作成を検討する。

## セキュリティ / 機密情報
- Discord Webhook URL などの機密情報は設定ファイル（`config.json`）で管理し、リポジトリには含めない。
- ログに Webhook URL や個人を特定できる情報を出力しない。

## ドキュメント更新
- コード変更に伴い、必要に応じてソースコード内のコメントを更新する。

## リポジトリ固有
- VRCX のデータベース（`VRCX.sqlite3`）の構造に依存しているため、スキーマ変更に注意する。
- Windows 環境専用（WinForms および UWP 通知を使用）。
