# AppConfig.cs レビュー

## 概要

`AppConfig.cs`はアプリケーションの設定を管理する静的クラスで、設定の読み込み、保存、取得、設定を担当します。設定データはJSON形式でファイルに保存されます。

## 良い点

1. 設定のカプセル化が適切で、アクセスと検証のロジックがプロパティに含まれている
2. 値の検証が組み込まれており、不正な値が設定されることを防止している
3. XMLドキュメントコメントが適切に記述されている
4. JSONシリアライズ/デシリアライズを使用して設定を永続化している

## 改善点

### 1. 設定の再読み込み

各プロパティのgetterで`Load()`が呼び出されていますが、これは非効率です。設定が変更されたことを検知するメカニズムが必要な場合は、より効率的な方法を検討すべきです。

```csharp
/// <summary>
/// 最後に設定ファイルを読み込んだ時刻
/// </summary>
private static DateTime _lastLoadTime = DateTime.MinValue;

/// <summary>
/// 設定ファイルが変更されたかどうかをチェックし、変更されていれば再読み込みする
/// </summary>
private static void CheckAndReload()
{
    if (!File.Exists(_configFilePath))
    {
        return;
    }

    var fileInfo = new FileInfo(_configFilePath);
    if (fileInfo.LastWriteTime > _lastLoadTime)
    {
        Load();
        _lastLoadTime = fileInfo.LastWriteTime;
    }
}

// プロパティの例
public static string DatabasePath
{
    get
    {
        CheckAndReload();
        return _config.DatabasePath;
    }
    // setter...
}
```

### 2. 例外処理の改善

設定ファイルの読み込み時の例外処理が不十分です。ファイルが存在しないケースは処理されていますが、ファイルが破損している場合やアクセス権限の問題など、他の例外については対応されていません。

```csharp
/// <summary>
/// 設定ファイルを読み込む
/// </summary>
private static void Load()
{
    if (!File.Exists(_configFilePath))
    {
        return;
    }

    try
    {
        var json = File.ReadAllText(_configFilePath);
        ConfigData? config = JsonSerializer.Deserialize<ConfigData>(json);
        if (config != null)
        {
            _config = config;
        }
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error reading config file: {ex.Message}");
        // ファイルの読み取りエラーログを記録
        // 必要に応じてデフォルト設定を使用
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Error parsing config file: {ex.Message}");
        // JSONパースエラーログを記録
        // 破損したファイルをバックアップ
        File.Move(_configFilePath, _configFilePath + ".bak", true);
        // デフォルト設定を使用
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error loading config: {ex.Message}");
        // 予期しないエラーログを記録
    }
}
```

### 3. スレッドセーフティの確保

現在の実装ではスレッドセーフティが確保されていません。複数のスレッドから同時に設定にアクセスされると、競合状態やデータ破損が発生する可能性があります。

```csharp
/// <summary>
/// 設定アクセス用のロックオブジェクト
/// </summary>
private static readonly object _configLock = new();

/// <summary>
/// 設定ファイルを読み込む
/// </summary>
private static void Load()
{
    lock (_configLock)
    {
        // 既存のLoad実装
    }
}

/// <summary>
/// 設定ファイルを保存する
/// </summary>
private static void Save()
{
    lock (_configLock)
    {
        // 既存のSave実装
    }
}

// プロパティの例
public static string DatabasePath
{
    get
    {
        lock (_configLock)
        {
            CheckAndReload();
            return _config.DatabasePath;
        }
    }
    set
    {
        lock (_configLock)
        {
            // 既存のsetter実装
        }
    }
}
```

### 4. 設定変更通知の実装

設定が変更されたことを通知するイベントがあると、アプリケーションの他の部分が設定変更に動的に対応できるようになります。

```csharp
/// <summary>
/// 設定変更イベントの引数
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    public string PropertyName { get; }
    
    public ConfigChangedEventArgs(string propertyName)
    {
        PropertyName = propertyName;
    }
}

/// <summary>
/// 設定変更イベント
/// </summary>
public static event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

/// <summary>
/// 設定変更イベントを発生させる
/// </summary>
private static void OnConfigChanged(string propertyName)
{
    ConfigChanged?.Invoke(null, new ConfigChangedEventArgs(propertyName));
}

// プロパティの例
public static string DatabasePath
{
    // getter...
    set
    {
        lock (_configLock)
        {
            var trimmedValue = value.Trim();
            // 検証ロジック...
            
            if (_config.DatabasePath != trimmedValue)
            {
                _config.DatabasePath = trimmedValue;
                Save();
                OnConfigChanged(nameof(DatabasePath));
            }
        }
    }
}
```

### 5. 設定ファイルパスのカスタマイズ

設定ファイルのパスが現在の実行ディレクトリにハードコードされています。ポータブルモードとインストールモードで異なるパスを使用できるようにすると良いでしょう。

```csharp
/// <summary>
/// 設定ファイルのパス
/// </summary>
private static readonly string _configFilePath = DetermineConfigFilePath();

/// <summary>
/// 設定ファイルのパスを決定する
/// </summary>
private static string DetermineConfigFilePath()
{
    // ポータブルモードのチェック（例：同じディレクトリにportable.txtが存在する場合）
    if (File.Exists(Path.Combine(Environment.CurrentDirectory, "portable.txt")))
    {
        return Path.Combine(Environment.CurrentDirectory, "config.json");
    }
    
    // インストールモード：ユーザーのAppDataフォルダに保存
    return Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppConstants.AppName,
        "config.json");
}
```

### 6. 静的クラスの明示

クラスが静的クラスとしての機能を持っていますが、明示的に`static`キーワードが使用されていません。

```csharp
/// <summary>
/// アプリケーションの設定を管理するクラス
/// </summary>
internal static class AppConfig
{
    // 既存の実装
}
```

### 7. DiscordWebhookUrlの検証強化

現在のURLチェックは単純なプレフィックスチェックのみですが、より厳密な検証を行うべきです。

```csharp
public static string DiscordWebhookUrl
{
    // getter...
    set
    {
        var trimmedValue = value.Trim();
        
        if (!string.IsNullOrEmpty(trimmedValue))
        {
            // Discord Webhook URLの形式チェック
            if (!Uri.TryCreate(trimmedValue, UriKind.Absolute, out var uri) ||
                !(uri.Scheme == "http" || uri.Scheme == "https") ||
                !uri.Host.EndsWith("discord.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.Contains("/api/webhooks/"))
            {
                throw new ArgumentException("Invalid Discord Webhook URL format.");
            }
        }
        
        _config.DiscordWebhookUrl = trimmedValue;
        Save();
        OnConfigChanged(nameof(DiscordWebhookUrl));
    }
}
```

## セキュリティ上の懸念点

1. 設定ファイルに機密情報（Discord Webhook URL）が平文で保存されている
2. 設定ファイルに対するアクセス制御が実装されていない
3. ファイル操作時の例外処理が不十分で、セキュリティ的な脆弱性につながる可能性がある

改善案：
```csharp
// 機密情報の暗号化
private static string EncryptSensitiveData(string data)
{
    // Windows Data Protection API (DPAPI) を使用した暗号化
    byte[] plainText = Encoding.UTF8.GetBytes(data);
    byte[] encryptedData = ProtectedData.Protect(plainText, null, DataProtectionScope.CurrentUser);
    return Convert.ToBase64String(encryptedData);
}

private static string DecryptSensitiveData(string encryptedData)
{
    // 復号化
    try
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
        byte[] plainText = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainText);
    }
    catch
    {
        // 復号化に失敗した場合は空の文字列を返す
        return string.Empty;
    }
}

// このメカニズムをDiscordWebhookUrlのプロパティに適用
```

## 総合評価

`AppConfig`クラスは基本的な機能を提供していますが、スレッドセーフティ、例外処理、設定変更通知、カスタムパス、セキュリティなどの面で改善の余地があります。特に、スレッドセーフティと例外処理は重要な問題であり、早急に対応すべきです。また、機密情報の保護も検討する必要があります。
