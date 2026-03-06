# CLAUDE.md

## 目的
Claude Code の作業方針とプロジェクト固有ルールを示します。

## 判断記録のルール
判断が必要な変更を行う場合は、以下の内容を記録してください。
- 判断内容の要約
- 検討した代替案
- 採用しなかった案とその理由
- 前提条件・仮定・不確実性
- 他エージェントによるレビュー可否

## プロジェクト概要
- **目的**: VRCX のデータベースを監視し、VRChat のアクティビティを Discord Webhook を介して通知する。
- **主な機能**:
    - VRCX SQLite データベースの監視（`instanceMembers`, `myLocations` クエリ）。
    - インスタンス種別やリージョンの解析。
    - Discord Embed による詳細な通知。
    - 設定画面とトレイアイコンによる操作。

## 重要ルール
- **会話言語**: 日本語
- **コミット規約**: [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)（description は日本語）
- **コメント言語**: 日本語
- **エラーメッセージ言語**: 英語

## 環境のルール
- **ブランチ命名**: [Conventional Branch](https://conventional-branch.github.io)（feat, fix 等の短縮形を使用）
- **GitHub リポジトリ調査**: 調査が必要な場合は一時ディレクトリに clone して検索する。
- **Renovate**: Renovate が作成した PR に対して追加コミットや更新を行わない。

## Git Worktree
新規ブランチを作成する場合は、以下の構成で Git Worktree を作成してください。
```text
.bare/
<ブランチ名>
```

## コード改修時のルール
- 日本語と英数字の間には半角スペースを挿入する。
- エラーメッセージの絵文字は、全体で統一感を持たせる（既存のパターンに従う）。
- 関数やインターフェースには日本語で docstring (XML ドキュメントコメント) を記載する。

## 相談ルール
- **Codex CLI**: 実装の詳細レビュー、局所的な設計相談、既存コードとの整合性確認に使用。
- **Gemini CLI**: 外部仕様の確認、最新技術情報の取得、広範なコンテキストの理解に使用。
- ユーザーや他エージェントからの指摘は真摯に受け止め、黙殺しないこと。

## 開発コマンド
```bash
# 依存関係の復元
dotnet restore VRCXDiscordTracker.sln

# ビルド
dotnet build VRCXDiscordTracker.sln /p:Configuration=Release

# 発行
dotnet publish VRCXDiscordTracker.sln -p:PublishProfile=Publish

# フォーマット修正
dotnet format VRCXDiscordTracker.sln
```

## アーキテクチャと主要ファイル
- **VRCXDiscordTracker/Core**: アプリケーションのコアロジック。
    - `VRChat/`: VRChat のインスタンスや場所に関する解析。
    - `VRCX/`: VRCX データベースへのクエリとデータ構造。
    - `Notification/`: Discord および Windows 通知の処理。
- **VRCXDiscordTracker.Updater**: アップデート用実行ファイル。

## 実装パターン
- **推奨**:
    - 非同期処理 (`Task`, `async/await`) の活用。
    - `AppConfig` を通じた設定値へのアクセス。
- **非推奨**:
    - UI スレッドでの長時間実行。
    - データベース接続の開きっぱなし。

## テスト
- 現時点では自動テストは導入されていない。手動での動作確認を徹底すること。

## ドキュメント更新ルール
- ソースコード内の XML ドキュメントコメントを常に最新の状態に保つ。

## 作業チェックリスト

### 新規改修時
1. プロジェクトを理解する。
2. 作業ブランチが適切であることを確認する。
3. 最新のリモートブランチに基づいた新規ブランチであることを確認する。
4. 指定されたパッケージマネージャー（dotnet）で依存関係をインストールする。

### コミット・プッシュ前
1. Conventional Commits に従っていることを確認する。
2. センシティブな情報（Webhook URL 等）が含まれていないことを確認する。
3. `dotnet format` でエラーがないことを確認する。
4. 動作確認を行う。

### PR 作成前
1. PR 作成の依頼があることを確認する。
2. コンフリクトの恐れがないことを確認する。

### PR 作成後
1. コンフリクトがないことを確認する。
2. PR 本文が最新の状態のみを網羅していることを確認する。
3. CI (`gh pr checks`) の結果を確認する。

## リポジトリ固有
- Windows 専用アプリであるため、Windows API や WinForms 特有の挙動に留意する。
