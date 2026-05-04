# GEMINI.md

## 目的
Gemini CLI 向けのコンテキストと作業方針を定義します。

## 出力スタイル
- **言語**: 日本語
- **トーン**: プロフェッショナルかつ簡潔な CLI スタイル
- **形式**: GitHub Flavored Markdown

## 共通ルール
- **会話言語**: 日本語
- **コミット規約**: [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)（description は日本語）
- **日本語と英数字の間**: 半角スペースを挿入

## プロジェクト概要
- **目的**: VRCX のデータベースから情報を取得し、VRChat の状態変更を Discord に通知する。
- **主な機能**: SQLite 監視、インスタンス解析、Discord 通知、自動アップデート。

## コーディング規約
- **フォーマット**: `dotnet format` に従う。
- **命名規則**: C# 標準規約。
- **コメント言語**: 日本語。
- **エラーメッセージ言語**: 英語。

## 開発コマンド
```bash
# 復元
dotnet restore VRCXDiscordTracker.sln

# ビルド
dotnet build VRCXDiscordTracker.sln

# フォーマット
dotnet format VRCXDiscordTracker.sln
```

## 注意事項
- 認証情報（Webhook URL）をコードやコミットに含めない。
- 既存のコードスタイルとディレクトリ構造を尊重する。
- Windows 特有のライブラリ（WinForms, UWP Notifications）を使用していることに留意する。

## リポジトリ固有
- VRCX の SQLite データベースの場所は、デフォルトで `%AppData%/VRCX/VRCX.sqlite3` です。
- インスタンス種別（Public, Friend+ 等）やリージョン（US, EU, JP）のパースロジックが含まれています。
