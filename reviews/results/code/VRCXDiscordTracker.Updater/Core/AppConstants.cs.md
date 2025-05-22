# コードレビュー: VRCXDiscordTracker.Updater/Core/AppConstants.cs

## 概要

このファイルはUpdaterアプリケーション用の定数を定義するクラスを含んでいます。アプリケーション名やバージョン情報など、アプリケーション全体で使用される値が定義されています。

## 良い点

- XMLドキュメントコメントが適切に使用されており、各定数の目的が明確です
- 定数は読み取り専用（`readonly`）で適切に宣言されています
- `Assembly.GetExecutingAssembly()`を使ってアプリケーション名とバージョンを動的に取得しています
- コメントにより、バージョン形式（Major.Minor.Patch）が明示されています

## 改善点

### 1. static クラスの宣言

```csharp
// 現在のコード
internal class AppConstants
{
    // 静的メンバー
}

// 改善案
internal static class AppConstants
{
    // 静的メンバー
}
```

`AppConstants`クラスは静的メンバーのみを含むため、クラス自体も`static`として宣言するべきです。これにより意図が明確になり、インスタンス化されないことが保証されます。

### 2. バージョニングロジックの分離

```csharp
// 現在のコード
public static readonly string AppVersionString = (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0)).ToString(3); // Major.Minor.Patch

// 改善案
/// <summary>
/// アプリケーションバージョン
/// </summary>
public static readonly Version AppVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

/// <summary>
/// アプリケーションバージョンの文字列（Major.Minor.Patch形式）
/// </summary>
public static readonly string AppVersionString = AppVersion.ToString(3);
```

バージョン情報をVersion型として保持することで、文字列変換が必要ない場合にも利用できます。

### 3. GitHubリポジトリ情報の追加

```csharp
/// <summary>
/// GitHub リポジトリのオーナー名
/// </summary>
public static readonly string GitHubRepoOwner = "tomacheese";

/// <summary>
/// GitHub リポジトリ名
/// </summary>
public static readonly string GitHubRepoName = "VRCXDiscordTracker";

/// <summary>
/// GitHub リポジトリの完全な名前
/// </summary>
public static readonly string GitHubRepoFullName = $"{GitHubRepoOwner}/{GitHubRepoName}";

/// <summary>
/// GitHub リポジトリのURL
/// </summary>
public static readonly string GitHubRepoUrl = $"https://github.com/{GitHubRepoFullName}";

/// <summary>
/// GitHub API URL for releases
/// </summary>
public static readonly string GitHubApiReleasesUrl = $"https://api.github.com/repos/{GitHubRepoFullName}/releases";
```

Updaterアプリケーションはこれらの情報を使用してGitHubリリースを取得するため、メインアプリケーションと同様に持っているべきです。

### 4. プロジェクト間の一貫性

メインアプリケーションとUpdaterアプリケーションの両方で、同じ定数を別々に宣言していることが問題です。両方のファイルで整合性が取れていない場合、メンテナンスが困難になります。現在、メインアプリケーションでは`GitHubRepoOwner`と`GitHubRepoName`が含まれていますが、Updaterでは含まれていません。

以下のいずれかのアプローチを検討すべきです：

#### a. 共通ライブラリの作成

```plaintext
VRCXDiscordTracker.Common/
  - AppConstants.cs  # 共通の定数を含む
```

#### b. プロジェクト間参照の設定

```csharp
using VRCXDiscordTrackerCore = VRCXDiscordTracker.Core;

namespace VRCXDiscordTracker.Updater.Core;
internal static class AppConstants
{
    // 共通定数はメインプロジェクトから参照
    public static string GitHubRepoOwner => VRCXDiscordTrackerCore.AppConstants.GitHubRepoOwner;
    public static string GitHubRepoName => VRCXDiscordTrackerCore.AppConstants.GitHubRepoName;
    
    // Updater固有の定数
    public static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
}
```

### 5. アプリケーションパス関連の定数追加

```csharp
/// <summary>
/// アプリケーションの実行ディレクトリ
/// </summary>
public static readonly string ApplicationDirectory = AppDomain.CurrentDomain.BaseDirectory;

/// <summary>
/// メインアプリケーションの実行ファイルパス
/// </summary>
public static readonly string MainApplicationPath = Path.Combine(ApplicationDirectory, "VRCXDiscordTracker.exe");

/// <summary>
/// バックアップディレクトリのパス
/// </summary>
public static readonly string BackupDirectory = Path.Combine(ApplicationDirectory, "Backup");

/// <summary>
/// 一時ダウンロードディレクトリ
/// </summary>
public static readonly string TempDownloadDirectory = Path.Combine(Path.GetTempPath(), "VRCXDiscordTracker_Update");

/// <summary>
/// ログファイルのパス
/// </summary>
public static readonly string LogFilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    AppName,
    "updater.log");
```

アップデート処理では、メインアプリケーションの実行ファイルパスや、バックアップ、一時ファイルなどのパス情報が頻繁に使用されるため、これらを定数として定義しておくと便利です。

### 6. アプリケーションのディスプレイ名を追加

```csharp
/// <summary>
/// アプリケーションの表示名
/// </summary>
public static readonly string AppDisplayName = "VRCXDiscordTracker Updater";
```

ユーザーに表示するアプリケーション名を別途定義することで、内部で使用する技術的な名前とユーザーに表示するフレンドリーな名前を分けることができます。

## コードスタイルとパフォーマンスの考慮

- バージョン取得のコードは、アプリケーションの起動時に1度だけ実行されるので、パフォーマンスへの影響はほとんどありません
- 定数の命名規則は一貫しており、適切です
- コードはシンプルで理解しやすい構造になっています

## セキュリティの考慮事項

- 特に外部からの入力を受け付けていないため、セキュリティ上の懸念点はほとんどありません
- ただし、GitHub API URLsを含める場合は、レート制限やAPI変更に対応できるよう設計を検討すべきです

## メインアプリケーションとの整合性

メインアプリケーション（VRCXDiscordTracker）のAppConstantsクラスにはGitHubリポジトリ関連の情報が含まれていますが、Updaterプロジェクトでは含まれていません。両プロジェクト間で一貫性を持たせるべきです。

## まとめ

`AppConstants.cs`は基本的によく設計されていますが、以下の点で改善の余地があります：

1. 静的クラスとして宣言すべき
2. バージョン情報の取得ロジックを分離
3. GitHubリポジトリ情報など、メインアプリケーションとの一貫性を確保
4. アップデート処理に関連するパス定数を追加
5. 長期的には共通ライブラリへの移行を検討
