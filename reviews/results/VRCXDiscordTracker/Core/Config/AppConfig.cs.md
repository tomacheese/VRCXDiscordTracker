# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/Config/AppConfig.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 164行（大規模）

## 概要
アプリケーション設定の管理を行うクラス。JSON形式での設定保存、各種プロパティの取得・設定機能を提供している。

## 総合評価
**スコア: 5/10**

基本機能は実装されているが、設計上の問題、パフォーマンス問題、エラーハンドリングの不備が深刻。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐☆☆☆
**Issues:**
- 静的クラスによるグローバル状態の管理
- プロパティアクセス毎のファイルI/O
- スレッドセーフティの欠如
- 単一責任の原則違反（設定管理 + ファイルI/O + 検証）

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐☆
**Good:**
- C#命名規約に準拠
- XMLドキュメンテーションが充実

**Issues:**
- 長いメソッドと複雑な分岐

### 3. セキュリティ上の問題 ⭐⭐☆☆☆
**Issues:**
- ファイルパス検証の不備
- 権限チェックなし
- 設定ファイルの暗号化なし
- パストラバーサル攻撃への対策不足

### 4. パフォーマンスの問題 ⭐☆☆☆☆
**Issues:**
- プロパティアクセス毎のディスクI/O
- 不要なファイル読み込み
- JSON解析の重複実行
- ファイルロック競合の可能性

### 5. 可読性・保守性 ⭐⭐⭐☆☆
**Good:**
- 明確なコメント

**Issues:**
- 複雑な条件分岐
- 重複コードパターン
- 設定項目の追加困難

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- 静的クラスでモック化困難
- ファイルI/Oへの強い依存
- グローバル状態の影響

## 具体的な問題点と改善提案

### 1. 【重要度：高】設計の根本的な改善
**問題**: 静的クラス、プロパティアクセス毎のファイルI/O、スレッドセーフティ欠如

**改善案**:
```csharp
/// <summary>
/// アプリケーション設定管理のインターフェース
/// </summary>
public interface IAppConfigService
{
    string DatabasePath { get; set; }
    string DiscordWebhookUrl { get; set; }
    bool NotifyOnStart { get; set; }
    bool NotifyOnExit { get; set; }
    int LocationCount { get; set; }
    
    Task LoadAsync();
    Task SaveAsync();
    void Reset();
}

/// <summary>
/// 設定管理サービスの実装
/// </summary>
internal class AppConfigService : IAppConfigService
{
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private ConfigData _config;
    private bool _isLoaded = false;

    public AppConfigService(string? configFilePath = null)
    {
        _configFilePath = configFilePath ?? Path.Combine(Environment.CurrentDirectory, "config.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        _config = new ConfigData();
    }

    public string DatabasePath
    {
        get => EnsureLoadedAndGet(() => _config.DatabasePath);
        set => SetAndSave(v => _config.DatabasePath = ValidateDatabasePath(v));
    }

    public string DiscordWebhookUrl
    {
        get => EnsureLoadedAndGet(() => _config.DiscordWebhookUrl);
        set => SetAndSave(v => _config.DiscordWebhookUrl = ValidateWebhookUrl(v));
    }

    public bool NotifyOnStart
    {
        get => EnsureLoadedAndGet(() => _config.NotifyOnStart);
        set => SetAndSave(v => _config.NotifyOnStart = v);
    }

    public bool NotifyOnExit
    {
        get => EnsureLoadedAndGet(() => _config.NotifyOnExit);
        set => SetAndSave(v => _config.NotifyOnExit = v);
    }

    public int LocationCount
    {
        get => EnsureLoadedAndGet(() => _config.LocationCount);
        set => SetAndSave(v => _config.LocationCount = ValidateLocationCount(v));
    }

    public async Task LoadAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _config = new ConfigData();
                _isLoaded = true;
                return;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            _config = JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            throw new ConfigurationException($"Failed to load configuration: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_config, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new ConfigurationException($"Failed to save configuration: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private T EnsureLoadedAndGet<T>(Func<T> getter)
    {
        if (!_isLoaded)
        {
            LoadAsync().GetAwaiter().GetResult();
        }
        return getter();
    }

    private void SetAndSave<T>(Action<T> setter)
    {
        if (!_isLoaded)
        {
            LoadAsync().GetAwaiter().GetResult();
        }
        setter();
        SaveAsync().GetAwaiter().GetResult();
    }

    // バリデーションメソッド...
    private string ValidateDatabasePath(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            return AppConstants.VRCXDefaultDatabasePath;

        if (!IsValidDatabasePath(trimmed))
            throw new ArgumentException($"Invalid database path: {trimmed}");

        return trimmed;
    }

    private string ValidateWebhookUrl(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            return string.Empty;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Discord webhook URL must be a valid HTTP/HTTPS URL");

        return trimmed;
    }

    private int ValidateLocationCount(int value)
    {
        if (value < 1 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Location count must be between 1 and 100");
        return value;
    }
}

public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 2. 【重要度：高】セキュリティの強化
**改善案**:
```csharp
/// <summary>
/// セキュアな設定管理サービス
/// </summary>
internal class SecureAppConfigService : IAppConfigService
{
    private readonly IFileSecurityService _fileSecurityService;
    
    public SecureAppConfigService(IFileSecurityService fileSecurityService)
    {
        _fileSecurityService = fileSecurityService ?? throw new ArgumentNullException(nameof(fileSecurityService));
    }

    public async Task LoadAsync()
    {
        // パス検証
        if (!IsSecurePath(_configFilePath))
            throw new SecurityException("Configuration file path is not secure");

        // 権限チェック
        if (!await _fileSecurityService.CanReadFileAsync(_configFilePath))
            throw new UnauthorizedAccessException("Insufficient permissions to read configuration file");

        // ファイル整合性チェック
        if (!await _fileSecurityService.VerifyFileIntegrityAsync(_configFilePath))
            throw new SecurityException("Configuration file integrity check failed");

        // 設定の復号化（必要に応じて）
        var encryptedContent = await File.ReadAllTextAsync(_configFilePath);
        var json = await _fileSecurityService.DecryptAsync(encryptedContent);
        
        _config = JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
    }

    private bool IsSecurePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var currentDir = Environment.CurrentDirectory;
            
            // パストラバーサル対策
            return fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase) &&
                   !fullPath.Contains("..") &&
                   Path.GetFileName(fullPath).Equals("config.json", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
```

### 3. 【重要度：中】パフォーマンスの最適化
**改善案**:
```csharp
/// <summary>
/// キャッシュ機能付き設定サービス
/// </summary>
internal class CachedAppConfigService : IAppConfigService
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private DateTime _lastLoaded = DateTime.MinValue;
    private readonly object _lockObject = new object();

    public async Task<T> GetConfigValueAsync<T>(string key, T defaultValue)
    {
        if (ShouldReloadCache())
        {
            await LoadAsync();
        }

        // キャッシュから値を取得
        return GetFromCache(key, defaultValue);
    }

    private bool ShouldReloadCache()
    {
        lock (_lockObject)
        {
            return DateTime.UtcNow - _lastLoaded > _cacheExpiration;
        }
    }

    // バッチ更新機能
    public async Task UpdateMultipleAsync(Dictionary<string, object> updates)
    {
        await _semaphore.WaitAsync();
        try
        {
            foreach (var update in updates)
            {
                ApplyConfigUpdate(update.Key, update.Value);
            }
            await SaveAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 4. 【重要度：中】設定スキーマの導入
**改善案**:
```csharp
/// <summary>
/// 設定スキーマの定義
/// </summary>
public static class ConfigSchema
{
    public static readonly Dictionary<string, ConfigPropertyInfo> Properties = new()
    {
        ["DatabasePath"] = new ConfigPropertyInfo<string>
        {
            DefaultValue = () => AppConstants.VRCXDefaultDatabasePath,
            Validator = path => IsValidDatabasePath(path),
            Description = "Path to VRCX database file"
        },
        ["DiscordWebhookUrl"] = new ConfigPropertyInfo<string>
        {
            DefaultValue = () => string.Empty,
            Validator = url => string.IsNullOrEmpty(url) || IsValidUrl(url),
            Description = "Discord webhook URL for notifications"
        },
        ["LocationCount"] = new ConfigPropertyInfo<int>
        {
            DefaultValue = () => 5,
            Validator = count => count >= 1 && count <= 100,
            Description = "Number of locations to track"
        }
    };
}

public abstract class ConfigPropertyInfo
{
    public abstract object GetDefaultValue();
    public abstract bool ValidateValue(object value);
    public string Description { get; init; } = string.Empty;
}

public class ConfigPropertyInfo<T> : ConfigPropertyInfo
{
    public Func<T> DefaultValue { get; init; } = () => default(T)!;
    public Func<T, bool> Validator { get; init; } = _ => true;

    public override object GetDefaultValue() => DefaultValue()!;
    public override bool ValidateValue(object value) => value is T typedValue && Validator(typedValue);
}
```

## 推奨されるNext Steps
1. 設計の根本的な見直し（高優先度）
2. セキュリティ強化の実装（高優先度）
3. パフォーマンス最適化（中優先度）
4. 設定スキーマの導入（中優先度）
5. 包括的な単体テストの追加（中優先度）

## コメント
現在の実装は機能的には動作しますが、プロダクション環境での使用には重大な問題があります。特にパフォーマンス問題（プロパティアクセス毎のファイルI/O）とスレッドセーフティの欠如は深刻です。設計を根本的に見直し、適切な抽象化とキャッシュ機能を導入することで、より堅牢で効率的な設定管理システムにする必要があります。