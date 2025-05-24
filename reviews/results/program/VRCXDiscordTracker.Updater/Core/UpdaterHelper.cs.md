# UpdaterHelper.cs レビュー

## 概要

`UpdaterHelper.cs`はアップデートプロセスをサポートするユーティリティクラスで、実行中のプロセスの終了とZIPファイルの展開という2つの主要な機能を提供しています。

## 現状のコード

```csharp
using System.Diagnostics;
using System.IO.Compression;

namespace VRCXDiscordTracker.Updater.Core;

/// <summary>
/// UpdaterHelper
/// </summary>
internal class UpdaterHelper
{
    /// <summary>
    /// 指定したプロセス名のプロセスを全て終了させる。まずは CloseMainWindow() を呼び、5秒待ってから Kill() を呼ぶ。
    /// </summary>
    /// <param name="processName">プロセス名</param>
    public static void KillProcesses(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        foreach (Process proc in processes)
        {
            try
            {
                Console.WriteLine($"Requesting close for process Id={proc.Id}...");
                if (proc.CloseMainWindow())
                {
                    if (proc.WaitForExit(5000))
                    {
                        Console.WriteLine($"Process Id={proc.Id} exited gracefully.");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Process Id={proc.Id} did not exit within 5s, killing...");
                    }
                }
                else
                {
                    Console.WriteLine($"Process Id={proc.Id} has no main window or refused close, killing...");
                }

                proc.Kill();
                proc.WaitForExit();
                Console.WriteLine($"Process Id={proc.Id} killed.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to stop process Id={proc.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 指定したパスの ZIP ファイルを展開する
    /// </summary>
    /// <param name="zipPath">ZIP ファイルのパス</param>
    /// <param name="targetFolder">展開先フォルダ</param>
    public static void ExtractZipToTarget(string zipPath, string targetFolder)
    {
        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            // ディレクトリは飛ばす
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var dest = Path.Combine(targetFolder, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, overwrite: true);
        }
    }
}
```

## レビュー内容

### 良い点

1. ✅ **詳細なログ出力**: プロセス終了の各段階でログが出力されており、デバッグが容易
2. ✅ **段階的なプロセス終了**: まず正常終了を試み、応答がない場合のみ強制終了する適切なアプローチ
3. ✅ **例外処理**: プロセス終了処理で発生する可能性のある例外を適切に捕捉
4. ✅ **リソース管理**: `using`ステートメントによる適切なリソース解放

### 問題点

1. ⚠️ **エラー処理の不足**: ZIPファイル展開時のエラー処理が不足しており、失敗時の対応が定義されていない
2. ⚠️ **パスのセキュリティ**: ZIP内のパス検証が不十分で、ディレクトリトラバーサル攻撃の可能性がある
3. ⚠️ **ログ出力の依存性**: コンソール出力に直接依存しており、ユニットテストやログレベルの制御が困難
4. ⚠️ **メソッドの返値**: 操作の成功/失敗を示す戻り値が無く、呼び出し元での結果確認が困難

### セキュリティ上の懸念

1. **ディレクトリトラバーサル脆弱性**: ZIP内のエントリパスが検証されていないため、予期しないディレクトリに書き込みが行われる可能性がある
2. **不適切な権限のプロセス終了**: 管理者権限で実行されている場合、ユーザーに属さないプロセスも終了させる可能性がある

### 推奨改善案

#### 1. パスのセキュリティ強化

```csharp
public static void ExtractZipToTarget(string zipPath, string targetFolder)
{
    using ZipArchive archive = ZipFile.OpenRead(zipPath);
    foreach (ZipArchiveEntry entry in archive.Entries)
    {
        // ディレクトリは飛ばす
        if (string.IsNullOrEmpty(entry.Name)) continue;

        // パスのセキュリティ検証
        var entryFullName = entry.FullName.Replace('\\', '/');
        if (entryFullName.StartsWith("/") || entryFullName.Contains("../") || entryFullName.Contains("..\\"))
        {
            Console.Error.WriteLine($"Warning: Skipping suspicious path: {entry.FullName}");
            continue;
        }

        try
        {
            var dest = Path.GetFullPath(Path.Combine(targetFolder, entry.FullName));
            
            // 展開先が対象ディレクトリ内にあることを確認
            if (!dest.StartsWith(Path.GetFullPath(targetFolder)))
            {
                Console.Error.WriteLine($"Security warning: Attempted path traversal detected: {entry.FullName}");
                continue;
            }
            
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error extracting {entry.FullName}: {ex.Message}");
            throw; // 呼び出し元でハンドリングするために例外を再スロー
        }
    }
}
```

#### 2. プロセス終了の権限とセキュリティ強化

```csharp
public static bool KillProcesses(string processName)
{
    bool allProcessesClosed = true;
    Process[] processes = Process.GetProcessesByName(processName);
    
    if (processes.Length == 0)
    {
        Console.WriteLine($"No processes found with name: {processName}");
        return true;
    }
    
    foreach (Process proc in processes)
    {
        try
        {
            // 現在のユーザーが所有するプロセスかどうかを確認するコードを追加
            Console.WriteLine($"Requesting close for process Id={proc.Id}...");
            
            if (proc.CloseMainWindow())
            {
                if (proc.WaitForExit(5000))
                {
                    Console.WriteLine($"Process Id={proc.Id} exited gracefully.");
                    continue;
                }
                else
                {
                    Console.WriteLine($"Process Id={proc.Id} did not exit within 5s, killing...");
                }
            }
            else
            {
                Console.WriteLine($"Process Id={proc.Id} has no main window or refused close, killing...");
            }

            proc.Kill();
            proc.WaitForExit();
            Console.WriteLine($"Process Id={proc.Id} killed.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to stop process Id={proc.Id}: {ex.Message}");
            allProcessesClosed = false;
        }
    }
    
    return allProcessesClosed;
}
```

#### 3. ログ出力の分離

ログ出力を専用のインターフェースに分離することで、テスト性と柔軟性を向上させます：

```csharp
public interface ILogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}

public class ConsoleLogger : ILogger
{
    public void Info(string message) => Console.WriteLine(message);
    public void Warning(string message) => Console.WriteLine($"WARNING: {message}");
    public void Error(string message) => Console.Error.WriteLine($"ERROR: {message}");
}

internal class UpdaterHelper
{
    private readonly ILogger _logger;
    
    public UpdaterHelper(ILogger logger)
    {
        _logger = logger ?? new ConsoleLogger();
    }
    
    public bool KillProcesses(string processName)
    {
        // ... 同様のコードで、Console.WriteLine の代わりに _logger.Info/Warning/Error を使用
    }
    
    public void ExtractZipToTarget(string zipPath, string targetFolder)
    {
        // ... 同様のコードで、Console.WriteLine の代わりに _logger.Info/Warning/Error を使用
    }
}
```

## 総合評価

`UpdaterHelper`クラスは基本的な機能を適切に実装していますが、セキュリティと堅牢性の面で改善が必要です。特にZIPファイル展開時のパス検証は重要なセキュリティ対策であり、優先的に対応すべきです。また、ログ出力の分離やメソッドの戻り値の追加により、テスト性と使いやすさが向上するでしょう。
