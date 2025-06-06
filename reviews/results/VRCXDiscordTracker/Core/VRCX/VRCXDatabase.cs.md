# コードレビュー結果

## ファイル情報
- **ファイルパス**: VRCXDiscordTracker/Core/VRCX/VRCXDatabase.cs
- **レビュー日時**: 2025-06-06
- **ファイルサイズ**: 209行（大規模）

## 概要
VRCXのSQLiteデータベースアクセス層。データ取得、SQLクエリ実行、埋め込みリソース管理を担当。

## 総合評価
**スコア: 6/10**

基本機能は適切だが、リソース管理、エラーハンドリング、設計パターンで改善が必要。

## 詳細評価

### 1. 設計・構造の評価 ⭐⭐⭐☆☆
**Good:**
- 明確な責任（データアクセス）
- 埋め込みSQLリソースの活用

**Issues:**
- IDisposable未実装
- 接続管理の不適切な設計
- DAOパターンの不完全な実装

### 2. コーディング規約の遵守状況 ⭐⭐⭐⭐☆
**Good:**
- C#命名規約準拠
- XMLドキュメンテーション充実

**Issues:**
- 長いメソッド
- 例外処理の一貫性不足

### 3. セキュリティ上の問題 ⭐⭐⭐⭐☆
**Good:**
- パラメータ化クエリ使用
- 読み取り専用接続

**Minor Issues:**
- SQLインジェクション対策の部分的不足

### 4. パフォーマンスの問題 ⭐⭐⭐☆☆
**Good:**
- 適切なSQLite使用

**Issues:**
- 接続の再利用不足
- メモリ効率の改善余地

### 5. 可読性・保守性 ⭐⭐⭐☆☆
**Good:**
- 明確なメソッド名

**Issues:**
- 複雑なデータマッピング
- エラーハンドリングの不統一

### 6. テスト容易性 ⭐⭐☆☆☆
**Issues:**
- データベース依存のテスト困難
- インターフェース抽象化不足

## 具体的な問題点と改善提案

### 1. 【重要度：高】適切なリソース管理とDI対応
**問題**: IDisposable未実装、接続管理不適切

**改善案**:
```csharp
/// <summary>
/// VRCX データベースアクセスのインターフェース
/// </summary>
public interface IVRCXDatabase : IDisposable
{
    Task<string> GetVRChatUserIdAsync(CancellationToken cancellationToken = default);
    Task<List<MyLocation>> GetMyLocationsAsync(string vrchatUserId, int locationCount, CancellationToken cancellationToken = default);
    Task<List<InstanceMember>> GetInstanceMembersAsync(string vrchatUserId, MyLocation myLocation, CancellationToken cancellationToken = default);
    Task OpenAsync(CancellationToken cancellationToken = default);
    Task CloseAsync();
}

/// <summary>
/// 改善されたVRCXデータベース実装
/// </summary>
internal class VRCXDatabase : IVRCXDatabase
{
    private readonly string _connectionString;
    private readonly ILogger<VRCXDatabase> _logger;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private SQLiteConnection? _connection;
    private bool _disposed = false;

    public VRCXDatabase(string databasePath, ILogger<VRCXDatabase> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath, nameof(databasePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!File.Exists(databasePath))
            throw new FileNotFoundException($"VRCX database file not found: {databasePath}");

        var builder = new SQLiteConnectionStringBuilder
        {
            DataSource = databasePath,
            ReadOnly = true,
            PoolingEnabled = true,
            DefaultTimeout = 30
        };
        _connectionString = builder.ToString();
    }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_connection?.State == ConnectionState.Open)
                return;

            _connection?.Dispose();
            _connection = new SQLiteConnection(_connectionString);
            await _connection.OpenAsync(cancellationToken);
            
            _logger.LogInformation("VRCX database connection opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open VRCX database connection");
            throw new VRCXDatabaseException("Failed to open database connection", ex);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<string> GetVRChatUserIdAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting VRChat user ID");
        
        const string sql = "SELECT value FROM configs WHERE key = 'config:lastuserloggedin'";
        
        try
        {
            using var command = new SQLiteCommand(sql, _connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result is string userId && !string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogDebug("Retrieved VRChat user ID: {UserId}", userId);
                return userId;
            }

            throw new VRCXDatabaseException("VRChat User ID not found in database");
        }
        catch (Exception ex) when (ex is not VRCXDatabaseException)
        {
            _logger.LogError(ex, "Error retrieving VRChat user ID");
            throw new VRCXDatabaseException("Failed to retrieve VRChat user ID", ex);
        }
    }

    public async Task<List<MyLocation>> GetMyLocationsAsync(string vrchatUserId, int locationCount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vrchatUserId, nameof(vrchatUserId));
        ArgumentOutOfRangeException.ThrowIfLessThan(locationCount, 1, nameof(locationCount));

        _logger.LogDebug("Getting my locations for user {UserId}, count: {LocationCount}", vrchatUserId, locationCount);

        var sql = await GetEmbeddedSqlAsync("VRCXDiscordTracker.Core.VRCX.Queries.myLocations.sql");
        var locations = new List<MyLocation>();

        try
        {
            using var command = new SQLiteCommand(sql, _connection);
            command.Parameters.AddWithValue(":target_user_id", vrchatUserId);
            command.Parameters.AddWithValue(":location_count", locationCount);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    var location = await MapMyLocationAsync(reader, cancellationToken);
                    locations.Add(location);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error mapping location data, skipping record");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my locations");
            throw new VRCXDatabaseException("Failed to retrieve my locations", ex);
        }

        // ローカルインスタンスを除外
        var filteredLocations = locations.Where(loc => !loc.LocationId.StartsWith("local:")).ToList();
        
        _logger.LogDebug("Retrieved {LocationCount} locations (filtered: {FilteredCount})", 
            locations.Count, filteredLocations.Count);
        
        return filteredLocations;
    }

    public async Task<List<InstanceMember>> GetInstanceMembersAsync(string vrchatUserId, MyLocation myLocation, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vrchatUserId, nameof(vrchatUserId));
        ArgumentNullException.ThrowIfNull(myLocation, nameof(myLocation));

        _logger.LogDebug("Getting instance members for location {LocationId}", myLocation.LocationId);

        var sanitizedUserId = SanitizeUserId(vrchatUserId);
        var friendTableName = $"{sanitizedUserId}_friend_log_current";
        
        var sqlTemplate = await GetEmbeddedSqlAsync("VRCXDiscordTracker.Core.VRCX.Queries.instanceMembers.sql");
        var sql = sqlTemplate.Replace("@{friendTableName}", friendTableName);
        
        var members = new List<InstanceMember>();

        try
        {
            using var command = new SQLiteCommand(sql, _connection);
            command.Parameters.AddWithValue(":join_created_at", FormatDateTime(myLocation.JoinCreatedAt.AddSeconds(-1)));
            command.Parameters.AddWithValue(":estimated_leave_created_at", FormatDateTime(myLocation.EstimatedLeaveCreatedAt?.AddSeconds(1)));
            command.Parameters.AddWithValue(":location", myLocation.LocationId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var member = await MapInstanceMemberAsync(reader, cancellationToken);
                members.Add(member);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instance members for location {LocationId}", myLocation.LocationId);
            throw new VRCXDatabaseException($"Failed to retrieve instance members for location {myLocation.LocationId}", ex);
        }

        _logger.LogDebug("Retrieved {MemberCount} members for location {LocationId}", members.Count, myLocation.LocationId);
        return members;
    }

    private async Task<MyLocation> MapMyLocationAsync(SQLiteDataReader reader, CancellationToken cancellationToken)
    {
        return new MyLocation
        {
            JoinId = reader.GetInt64(0),
            UserId = reader.GetString(1),
            DisplayName = reader.GetString(2),
            LocationId = reader.GetString(3),
            LocationInstance = LocationParser.Parse(reader.GetString(3)),
            JoinCreatedAt = ParseDateTime(reader.GetString(4)),
            JoinTime = reader.GetInt64(5),
            LeaveId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
            LeaveCreatedAt = reader.IsDBNull(7) ? null : ParseDateTime(reader.GetString(7)),
            LeaveTime = reader.IsDBNull(8) ? null : reader.GetInt64(8),
            NextJoinCreatedAt = reader.IsDBNull(9) ? null : ParseDateTime(reader.GetString(9)),
            EstimatedLeaveCreatedAt = reader.IsDBNull(10) ? null : ParseDateTime(reader.GetString(10)),
            WorldName = reader.IsDBNull(11) ? null : reader.GetString(11),
            WorldId = reader.IsDBNull(12) ? null : reader.GetString(12),
        };
    }

    private async Task<InstanceMember> MapInstanceMemberAsync(SQLiteDataReader reader, CancellationToken cancellationToken)
    {
        return new InstanceMember
        {
            UserId = reader.GetString(0),
            DisplayName = reader.GetString(1),
            LastJoinAt = reader.IsDBNull(2) ? null : ParseDateTime(reader.GetString(2)),
            LastLeaveAt = reader.IsDBNull(3) ? null : ParseDateTime(reader.GetString(3)),
            IsCurrently = reader.GetBoolean(4),
            IsInstanceOwner = reader.GetBoolean(5),
            IsFriend = reader.GetBoolean(6),
        };
    }

    public async Task CloseAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
                _connection = null;
                _logger.LogInformation("VRCX database connection closed");
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private static async Task<string> GetEmbeddedSqlAsync(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(resourceName) 
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static string SanitizeUserId(string userId) => userId.Replace("_", "").Replace("-", "");

    private static DateTime ParseDateTime(string dateTimeString) => 
        DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture);

    private static string? FormatDateTime(DateTime? dateTime) => 
        dateTime?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            CloseAsync().GetAwaiter().GetResult();
            _connectionSemaphore?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// VRCX データベース例外
/// </summary>
public class VRCXDatabaseException : Exception
{
    public VRCXDatabaseException(string message) : base(message) { }
    public VRCXDatabaseException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 2. 【重要度：中】リポジトリパターンの導入
**改善案**:
```csharp
/// <summary>
/// VRCX データリポジトリ
/// </summary>
public interface IVRCXRepository
{
    Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<MyLocation>> GetRecentLocationsAsync(string userId, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<InstanceMember>> GetLocationMembersAsync(string userId, MyLocation location, CancellationToken cancellationToken = default);
}

/// <summary>
/// 高レベルのVRCXサービス
/// </summary>
internal class VRCXService : IVRCXService
{
    private readonly IVRCXRepository _repository;
    private readonly ILogger<VRCXService> _logger;

    public VRCXService(IVRCXRepository repository, ILogger<VRCXService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LocationSummary> GetLocationSummaryAsync(int locationCount, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetCurrentUserAsync(cancellationToken);
        var locations = await _repository.GetRecentLocationsAsync(user.Id, locationCount, cancellationToken);
        
        var locationDetails = new List<LocationDetail>();
        
        foreach (var location in locations)
        {
            var members = await _repository.GetLocationMembersAsync(user.Id, location, cancellationToken);
            locationDetails.Add(new LocationDetail
            {
                Location = location,
                Members = members.ToList(),
                MemberCount = members.Count()
            });
        }

        return new LocationSummary
        {
            User = user,
            Locations = locationDetails
        };
    }
}
```

## 推奨されるNext Steps
1. IDisposableパターンの実装（高優先度）
2. 非同期処理への完全移行（高優先度）
3. 包括的なエラーハンドリング（高優先度）
4. リポジトリパターンの導入（中優先度）
5. 包括的な単体テストの追加（中優先度）

## コメント
SQLiteアクセスの基本機能は適切に実装されていますが、現代的なデータアクセス層としては改善が必要です。特にリソース管理の問題と同期処理の使用は、スケーラビリティと安定性に影響します。非同期処理への移行とRepositoryパターンの導入により、よりテスタブルで保守しやすい設計にする必要があります。