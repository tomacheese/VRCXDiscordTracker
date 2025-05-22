# コードレビュー: VRCXDiscordTracker.Updater/Program.cs

## 概要

このファイルはVRCXDiscordTrackerアプリケーションのアップデーターのエントリーポイントです。GitHubからリリースを取得し、アプリケーションを最新バージョンに更新する機能を提供しています。

## 良い点

- コマンドライン引数のパースが適切に実装されています。
- 一時フォルダへの自己コピーと再起動のロジックが実装されており、実行中のファイルを上書きする問題を回避しています。
- 更新プロセスの各ステップがコンソールに出力され、進行状況が明確です。
- エラー発生時にアプリケーションをアップデートスキップモードで起動する回避策が実装されています。

## 改善点

### 1. コードの構造化

```csharp
// メインメソッドが長く、複数の責務を持っています
static async Task Main(string[] args)
{
    // 150行以上のコード...
}

// メソッドを機能ごとに分割するべきです
static async Task Main(string[] args)
{
    try
    {
        // 引数のパースと検証
        var arguments = ParseAndValidateArguments(args);
        
        // 自己コピーと再起動のチェック
        if (await CheckAndRelaunchIfNeeded(arguments))
            return;
            
        // 更新プロセスの実行
        await PerformUpdate(arguments);
    }
    catch (Exception ex)
    {
        await HandleUpdateError(ex, args);
    }
}

// 分割されたメソッドの実装
private static UpdateArguments ParseAndValidateArguments(string[] args) { ... }
private static async Task<bool> CheckAndRelaunchIfNeeded(UpdateArguments args) { ... }
private static async Task PerformUpdate(UpdateArguments args) { ... }
private static async Task HandleUpdateError(Exception ex, UpdateArguments args) { ... }

// 引数を保持するクラス
private class UpdateArguments
{
    public string AppName { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string RepoOwner { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
}
```

### 2. エラー処理

```csharp
// 全ての例外を単一のcatchブロックで処理しています
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    // ...
}

// 異なる種類の例外を個別に処理するべきです
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Invalid arguments: {ex.Message}");
    DisplayUsage();
}
catch (IOException ex)
{
    Console.Error.WriteLine($"File operation error: {ex.Message}");
    // ファイル操作に関するエラー処理
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Network error: {ex.Message}");
    // ネットワークエラーに関する処理
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    // その他の予期しない例外の処理
}
```

### 3. コマンドライン引数の検証

```csharp
// 複数の引数を一度に検証しています
if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(repoOwner) || string.IsNullOrEmpty(repoName))
{
    throw new ArgumentException("Invalid arguments. Required: --app-name=<AppName> --target=<TargetFolder> --asset-name=<AssetName> --repo-owner=<RepoOwner> --repo-name=<RepoName>");
}

// 各引数を個別に検証し、具体的なエラーメッセージを提供するべきです
private static void ValidateArguments(UpdateArguments args)
{
    if (string.IsNullOrEmpty(args.AppName))
        throw new ArgumentException("Application name must be specified. Use --app-name=<AppName>");
        
    if (string.IsNullOrEmpty(args.Target))
        throw new ArgumentException("Target folder must be specified. Use --target=<TargetFolder>");
        
    // 他の引数も同様に検証
}
```

### 4. リソース管理

```csharp
// Process.Startが呼び出されますが、戻り値のProcessオブジェクトが破棄されていません
Process.Start(new ProcessStartInfo
{
    FileName = selfCopyExe,
    UseShellExecute = false,
    ArgumentList = {
        // 引数リスト
    },
});

// Processオブジェクトを適切に管理するべきです
using var process = Process.Start(new ProcessStartInfo
{
    // 設定
});
```

### 5. ハードコードされた文字列

```csharp
// フォルダパスの構築に文字列連結を使用しています
var tempRoot = Path.Combine(Path.GetTempPath(), appName, "Updater");
var selfCopyExe = Path.Combine(versionFolder, Path.GetFileName(currentExe));

// 一貫した方法で構築するべきです
private static string GetTemporaryPath(string appName)
{
    return Path.Combine(
        Path.GetTempPath(),
        appName,
        "Updater");
}
```

## セキュリティの問題

- ファイルパスやプロセス名が検証されておらず、悪意のあるコマンドライン引数が渡された場合にリスクがあります。パスの検証とサニタイズを実装するべきです。
- ZIPファイルの展開前に内容を検証していないため、潜在的なセキュリティリスクがあります。ファイルの整合性チェックを検討してください。

## パフォーマンスの問題

- アプリケーションのプロセスを終了するためにKillProcessesメソッドを使用していますが、これは正常な終了を保証しません。より安全なシャットダウンメカニズムを検討するべきです。

## テスト容易性

- 直接的なファイルシステム操作、プロセス管理、ネットワーク操作が行われており、単体テストが困難です。これらの操作をインターフェースを通じて抽象化することで、テスト容易性を向上させることができます。

## その他のコメント

- コマンドライン引数のパースに専用のライブラリ（例：CommandLineParser）を使用することで、より堅牢で明確なコードになる可能性があります。
- アップデートの進行状況が単純なコンソール出力のみです。UI表示や進捗バーなどを検討して、ユーザーエクスペリエンスを向上させるとよいでしょう。
