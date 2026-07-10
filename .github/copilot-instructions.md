# GitHub Copilot コードレビュー指示

VRCXDiscordTracker のプルリクエストをレビューする際の観点。C# / .NET 9.0（WinForms）、VRCX の SQLite DB を監視し Discord Webhook で通知する Windows 常駐アプリ。

## 重点的に確認する点

- **機密情報の混入**: Discord Webhook URL、`config.json` の実値、個人を特定できる VRChat 情報がコード・テストデータ・ログ・コミットに含まれていないか。ログ出力に Webhook URL やユーザー情報が漏れていないか。
- **リソース解放**: `SQLiteConnection` / `SQLiteCommand` / `SQLiteDataReader` が `using` で確実に破棄されているか。DB 接続の開きっぱなしは指摘対象。
- **SQL の扱い**: VRCX DB へのクエリは埋め込み SQL（`Core/VRCX/Queries/`）。動的に組み立てる値がある場合はパラメータ化されているか。
- **非同期と UI スレッド**: `async/await` が適切に使われ、UI スレッドを長時間ブロックしていないか。WinForms コントロールへスレッド外からアクセスしていないか（`Invoke`/`BeginInvoke` の要否）。
- **null 安全性**: Nullable 有効。null 許容警告を握りつぶさず、DB 読み取り結果や設定値の null を適切に扱っているか。
- **VRCX スキーマ依存**: インスタンス種別・リージョンの解析（`Core/VRChat/`）や DB クエリが VRCX のスキーマ前提に依存する。パースの分岐漏れや未知値のフォールバックがあるか。
- **設定アクセス**: 設定値は `AppConfig` 経由か（直接ファイルを読む重複実装を持ち込んでいないか）。

## 規約（lint/format で強制済み）

- **フォーマット**: `dotnet format` を CI が `--verify-no-changes --severity warn` で検証する。整形差分は CI で落ちるため、スタイルの微修正はレビューで重ねて指摘しなくてよい。
- **コミット**: [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)（description は日本語）。
- **コメント言語**: 公開メソッド・インターフェースの XML ドキュメントコメントは日本語。エラーメッセージは英語。日本語と英数字の間は半角スペース。

## フラグ不要な既知パターン（誤検知しやすい点）

- **Windows 専用 API**: WinForms・UWP トースト通知（`Microsoft.Toolkit.Uwp.Notifications`）はクロスプラットフォーム非対応で意図的。移植性の指摘は不要。
- **`AllowUnsafeBlocks`**: csproj で有効化済み。`unsafe` の利用自体は設定上許容されている。
- **自動テストの不在**: テストプロジェクトは未整備。テスト追加要求は必須指摘としない（新規ロジックで妥当な場合のみ提案）。
- **`config.json` は実行ディレクトリ**: `%APPDATA%` ではなく実行ディレクトリ配置は仕様（`AppConfig`）。パス選択の指摘は不要。
