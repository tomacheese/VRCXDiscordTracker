# VRCXDiscordTracker

[VRCX](https://github.com/vrcx-team/VRCX) のデータベースから参加したインスタンスの情報とメンバーを取得し、Discord に投稿するアプリケーションです。

## 機能

- VRCX データベースから訪問したインスタンス情報を取得
- インスタンスに参加していたメンバー情報を取得
- Discord Webhook を通じて情報を投稿
- Windows 通知のサポート

## 必要要件

- Windows 10 以降（バージョン 1809 以上）
- .NET 9.0 Runtime
- [VRCX](https://github.com/vrcx-team/VRCX) がインストールされていること

## インストール

[Releases](https://github.com/tomacheese/VRCXDiscordTracker/releases) から最新版をダウンロードして実行してください。

## 開発者向け

### ビルド

```bash
# リポジトリのクローン
git clone https://github.com/tomacheese/VRCXDiscordTracker.git
cd VRCXDiscordTracker

# ビルド
dotnet build

# 実行
dotnet run --project VRCXDiscordTracker
```

### 公開用ビルド

```bash
dotnet publish -c Release
```

## ライセンス

このプロジェクトは [MIT](LICENSE) ライセンスの下で公開されています。
