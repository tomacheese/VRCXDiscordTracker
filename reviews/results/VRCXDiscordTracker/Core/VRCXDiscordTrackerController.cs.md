# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/VRCXDiscordTrackerController.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 124行（大規模）

## 概要
アプリケーションのメインコントローラー。VRCX監視、定期処理、Discord通知の統合制御を担当。

## 総合評価
**スコア: 5/10**

アプリケーションの中核機能だが、設計・エラーハンドリング・リソース管理で深刻な問題。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐☆☆☆
**Issues:**
- 単一クラスに複数責任集中
- 同期的なTimer使用でTask.Run.Wait()のアンチパターン
- 適切なライフサイクル管理の欠如
- IDisposable未実装

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐☆
**Good:**
- C#命名規約準拠
- XMLドキュメンテーション適切

**Issues:**
- 非推奨パターンの使用

### 3. セキュリティ上の問題 ⭐⭐⭐☆☆
**Issues:**
- 例外情報の漏洩
- データベース接続の不適切な管理

### 4. パフォーマンスの問題 ⭐⭐☆☆☆
**Issues:**
- Task.Run().Wait()によるデッドロック危険性
- Windows.FormsタイマーでのI/O重い処理
- 例外時のスタックトレース重複出力

### 5. 可読性・保守性 ⭐⭐⭐☆☆
**Good:**
- 明確な命名

**Issues:**
- 複雑な責任の混在
- エラーハンドリング不完全

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- 具象クラスへの強い依存
- タイマーとデータベースの密結合

## 具体的な問題点と改善提案

### 1. 【重要度：高】設計の根本的改善
**問題**: 責任の混在、同期/非同期の混合、適切でないタイマー使用

**改善案**:
```csharp
/// <summary>
/// 改善されたコントローラー（責任分離・非同期対応）
/// </summary>
internal class VRCXDiscordTrackerController : IDisposable
{
    private readonly IVRCXService _vrcxService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<VRCXDiscordTrackerController> _logger;
    private readonly Timer _monitoringTimer;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public VRCXDiscordTrackerController(
        IVRCXService vrcxService,
        INotificationService notificationService,
        ILogger<VRCXDiscordTrackerController> logger,
        TrackerSettings settings)
    {
        _vrcxService = vrcxService ?? throw new ArgumentNullException(nameof(vrcxService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _monitoringTimer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        
        MonitoringInterval = settings.MonitoringInterval;
        IsRunning = false;
    }

    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(5);
    public bool IsRunning { get; private set; }

    public async Task StartAsync()
    {
        if (IsRunning) return;

        try
        {
            _logger.LogInformation("Starting VRCX Discord Tracker");
            
            await _vrcxService.InitializeAsync(_cancellationTokenSource.Token);
            
            _monitoringTimer.Change(TimeSpan.Zero, MonitoringInterval);
            IsRunning = true;
            
            _logger.LogInformation("VRCX Discord Tracker started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start tracker");
            throw new TrackerException("Failed to start tracker", ex);
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        try
        {
            _logger.LogInformation("Stopping VRCX Discord Tracker");
            
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // 進行中の処理が完了するまで待機
            await _processingSemaphore.WaitAsync(TimeSpan.FromSeconds(30), _cancellationTokenSource.Token);
            
            await _vrcxService.CloseAsync();
            
            IsRunning = false;
            
            _logger.LogInformation("VRCX Discord Tracker stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping tracker");
        }
    }

    private async void OnTimerElapsed(object? state)
    {
        // セマフォで同時実行を防止
        if (!await _processingSemaphore.WaitAsync(100))
        {
            _logger.LogDebug("Previous monitoring operation still in progress, skipping");
            return;
        }

        try
        {
            await PerformMonitoringAsync(_cancellationTokenSource.Token);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    private async Task PerformMonitoringAsync(CancellationToken cancellationToken)
    {
        try
        {
            var monitoringResult = await _vrcxService.GetLatestLocationDataAsync(cancellationToken);
            
            if (monitoringResult.Locations.Any())
            {
                await _notificationService.ProcessLocationsAsync(monitoringResult.Locations, cancellationToken);
                _logger.LogDebug("Processed {LocationCount} locations", monitoringResult.Locations.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Monitoring operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during monitoring operation");
            
            // 重要でないエラーの場合は継続、重要なエラーの場合は停止検討
            if (IsRetriableError(ex))
            {
                _logger.LogWarning("Retriable error occurred, continuing monitoring");
            }
            else
            {
                _logger.LogCritical("Critical error occurred, stopping tracker");
                await StopAsync();
            }
        }
    }

    private static bool IsRetriableError(Exception ex)
    {
        return ex is not (UnauthorizedAccessException or FileNotFoundException or SecurityException);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource.Cancel();
            
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
            
            _monitoringTimer?.Dispose();
            _processingSemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}

/// <summary>
/// トラッカー例外
/// </summary>
public class TrackerException : Exception
{
    public TrackerException(string message) : base(message) { }
    public TrackerException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 2. 【重要度：高】サービス層の分離
**改善案**:
```csharp
/// <summary>
/// VRCXサービスインターフェース
/// </summary>
public interface IVRCXService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<MonitoringResult> GetLatestLocationDataAsync(CancellationToken cancellationToken = default);
    Task CloseAsync();
    string GetDatabasePath();
    string GetUserId();
}

/// <summary>
/// VRCXサービス実装
/// </summary>
internal class VRCXService : IVRCXService
{
    private readonly string _databasePath;
    private readonly IAppConfigService _configService;
    private readonly ILogger<VRCXService> _logger;
    private VRCXDatabase? _database;

    public VRCXService(string databasePath, IAppConfigService configService, ILogger<VRCXService> logger)
    {
        _databasePath = !string.IsNullOrWhiteSpace(databasePath) 
            ? databasePath 
            : AppConstants.VRCXDefaultDatabasePath;
        _configService = configService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _database = new VRCXDatabase(_databasePath);
            await Task.Run(() => _database.Open(), cancellationToken);
            _logger.LogInformation("VRCX database connection established: {DatabasePath}", _databasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize VRCX database connection");
            throw new VRCXServiceException("Database initialization failed", ex);
        }
    }

    public async Task<MonitoringResult> GetLatestLocationDataAsync(CancellationToken cancellationToken = default)
    {
        if (_database == null)
            throw new InvalidOperationException("VRCX service not initialized");

        try
        {
            var userId = await Task.Run(() => _database.GetVRChatUserId(), cancellationToken);
            var locationCount = await _configService.GetLocationCountAsync();
            
            var locations = await Task.Run(() => 
                _database.GetMyLocations(userId, locationCount), cancellationToken);

            var result = new MonitoringResult
            {
                UserId = userId,
                Locations = new List<LocationWithMembers>()
            };

            foreach (var location in locations)
            {
                var members = await Task.Run(() => 
                    _database.GetInstanceMembers(userId, location), cancellationToken);
                
                result.Locations.Add(new LocationWithMembers
                {
                    Location = location,
                    Members = members
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get location data from VRCX database");
            throw new VRCXServiceException("Failed to retrieve location data", ex);
        }
    }

    public async Task CloseAsync()
    {
        if (_database != null)
        {
            await Task.Run(() =>
            {
                _database.Close();
                _database.Dispose();
            });
            _database = null;
        }
    }

    public string GetDatabasePath() => _databasePath;
    public string GetUserId() => _database?.GetVRChatUserId() ?? throw new InvalidOperationException("Service not initialized");
}

/// <summary>
/// 監視結果
/// </summary>
public class MonitoringResult
{
    public string UserId { get; set; } = string.Empty;
    public List<LocationWithMembers> Locations { get; set; } = new();
}

/// <summary>
/// 位置情報とメンバー情報
/// </summary>
public class LocationWithMembers
{
    public MyLocation Location { get; set; } = null!;
    public List<InstanceMember> Members { get; set; } = new();
}

public class VRCXServiceException : Exception
{
    public VRCXServiceException(string message) : base(message) { }
    public VRCXServiceException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 3. 【重要度：中】設定の外部化
**改善案**:
```csharp
/// <summary>
/// トラッカー設定
/// </summary>
public class TrackerSettings
{
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool StopOnCriticalError { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// 設定ベースのファクトリー
/// </summary>
internal static class TrackerControllerFactory
{
    public static VRCXDiscordTrackerController Create(
        string databasePath,
        TrackerSettings? settings = null,
        IServiceProvider? serviceProvider = null)
    {
        settings ??= new TrackerSettings();
        
        var vrcxService = serviceProvider?.GetService<IVRCXService>() ?? 
                         new VRCXService(databasePath, 
                                       serviceProvider?.GetRequiredService<IAppConfigService>()!,
                                       serviceProvider?.GetRequiredService<ILogger<VRCXService>>()!);
        
        var notificationService = serviceProvider?.GetService<INotificationService>() ??
                                 throw new InvalidOperationException("Notification service not configured");
        
        var logger = serviceProvider?.GetService<ILogger<VRCXDiscordTrackerController>>() ??
                    throw new InvalidOperationException("Logger not configured");

        return new VRCXDiscordTrackerController(vrcxService, notificationService, logger, settings);
    }
}
```

## 推奨されるNext Steps
1. 責任分離とDI導入（高優先度）
2. 非同期処理の適切な実装（高優先度）
3. 包括的なエラーハンドリング（高優先度）
4. IDisposableの適切な実装（中優先度）
5. 包括的な単体テストの追加（中優先度）

## コメント
アプリケーションの中核を担う重要なクラスですが、現在の実装は複数の深刻な問題を抱えています。特にTask.Run().Wait()パターンは絶対に避けるべきアンチパターンです。責任分離とDI導入により、よりテスタブルで保守しやすいアーキテクチャにする必要があります。