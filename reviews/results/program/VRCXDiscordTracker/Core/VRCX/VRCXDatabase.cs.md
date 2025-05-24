# VRCXDatabase.cs レビュー

## 概要

`VRCXDatabase.cs`はVRCXのSQLiteデータベースに接続し、必要なデータを取得するためのデータアクセスクラスです。VRChatユーザーIDの取得、ユーザーの移動履歴（位置情報）の取得、インスタンス内のメンバー情報の取得などの機能を提供しています。

## 良い点

1. SQLクエリを分離して外部ファイルとして管理している
2. パラメータ化クエリを使用してSQLインジェクションを防止している
3. 例外処理と適切なロギングが含まれている
4. 各メソッドに適切なドキュメントコメントが付けられている
5. データベース接続をreadonly（読み取り専用）に設定しており、データの誤修正を防止している

## 改善点

### 1. IDisposableインターフェースの実装

クラスは`Dispose`メソッドを持っていますが、`IDisposable`インターフェースを実装していません。

```csharp
/// <summary>
/// VRCXのSQLiteデータベースを操作するクラス
/// </summary>
internal class VRCXDatabase : IDisposable
{
    // 既存のコード

    /// <summary>
    /// データベース接続を破棄する
    /// </summary>
    public void Dispose()
    {
        Console.WriteLine("VRCXDatabase.Dispose()");
        _conn.Close();
        _conn.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// ファイナライザー
    /// </summary>
    ~VRCXDatabase()
    {
        Dispose();
    }
}
```

### 2. 接続状態の確認プロパティの追加

データベース接続の状態を確認するためのプロパティがあると便利です。

```csharp
/// <summary>
/// データベース接続が開いているかどうかを取得します
/// </summary>
public bool IsOpen => _conn.State == System.Data.ConnectionState.Open;
```

### 3. SQLクエリの例外処理の改善

SQLクエリ実行時の例外処理が部分的にしか行われていません。`GetVRChatUserId`メソッドなどでは例外が上位に伝播されますが、`GetMyLocations`メソッドでは例外をキャッチして出力するだけで続行しています。一貫した例外処理戦略が必要です。

```csharp
public string GetVRChatUserId()
{
    Console.WriteLine("VRCXDatabase.GetVRChatUserId()");
    try
    {
        using var cmd = new SQLiteCommand(_conn);
        cmd.CommandText = "SELECT value FROM configs WHERE key = 'config:lastuserloggedin'";
        using SQLiteDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetString(0);
        }

        throw new VRCXDatabaseException("VRChat User ID not found in database.");
    }
    catch (SQLiteException ex)
    {
        throw new VRCXDatabaseException("Error while fetching VRChat User ID", ex);
    }
}
```

カスタム例外クラスの定義：

```csharp
/// <summary>
/// VRCXデータベース操作に関する例外
/// </summary>
public class VRCXDatabaseException : Exception
{
    public VRCXDatabaseException(string message) : base(message) { }
    public VRCXDatabaseException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 4. トランザクション管理の検討

現在のコードはトランザクションを使用していません。読み取り専用操作なので必須ではありませんが、複数のクエリを実行する場合にはトランザクションを使用することで一貫性が保証されます。

```csharp
public List<MyLocation> GetMyLocations(string vrchatUserId, int locationCount)
{
    Console.WriteLine($"VRCXDatabase.GetMyLocations(): {vrchatUserId}");
    var sql = GetEmbedFileContent("VRCXDiscordTracker.Core.VRCX.Queries.myLocations.sql");

    var myLocations = new List<MyLocation>();
    using (var transaction = _conn.BeginTransaction())
    {
        try
        {
            using (var cmd = new SQLiteCommand(_conn))
            {
                cmd.Transaction = transaction;
                // 既存のコード
            }
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"Error in GetMyLocations: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw new VRCXDatabaseException("Failed to get location data", ex);
        }
    }

    return myLocations.FindAll(
        myLocation => !myLocation.LocationId.StartsWith("local:")
    );
}
```

### 5. パフォーマンス向上のための接続プールの検討

現在は単一の接続を使い回していますが、複数スレッドからのアクセスを考慮する場合は接続プールの使用を検討すべきです。

```csharp
// 静的コンストラクタでプール設定
static VRCXDatabase()
{
    // 接続プールの有効化
    SQLiteConnectionStringBuilder.AutomaticEnlistment = true;
    SQLiteConnectionStringBuilder.PoolSize = 10;
}
```

### 6. DateTime処理の改善

日時解析がCultureInfoに依存しており、異なるロケール環境で問題が発生する可能性があります。

```csharp
// 現在のコード
JoinCreatedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture),

// 改善案：より堅牢なDateTime.TryParse使用
if (DateTime.TryParse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var joinDate))
{
    myLocation.JoinCreatedAt = joinDate;
}
else
{
    throw new FormatException($"Invalid date format: {reader.GetString(4)}");
}
```

### 7. ログ出力の改善

現在は`Console.WriteLine`を使用していますが、構造化ログを使用するとより良いです。

```csharp
private static readonly ILogger Logger = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
}).CreateLogger<VRCXDatabase>();

// 使用例
Logger.LogInformation("VRCXDatabase.Open()");
Logger.LogDebug($"GetVRChatUserId: {userId}");
Logger.LogError($"Error: {ex.Message}", ex);
```

### 8. リソース解放のタイミング確認

`Dispose`メソッドで`Close`を呼び出した後に`Dispose`を呼び出していますが、`SQLiteConnection`の`Dispose`内部で既に`Close`が呼ばれている可能性があります。SQLiteConnectionのドキュメントを確認して、必要な場合のみ`Close`を呼び出すようにします。

```csharp
public void Dispose()
{
    Console.WriteLine("VRCXDatabase.Dispose()");
    if (_conn != null)
    {
        _conn.Dispose(); // Dispose内でCloseも呼ばれるはず
    }
    GC.SuppressFinalize(this);
}
```

### 9. 埋め込みリソース読み込みの最適化

`GetEmbedFileContent`メソッドがクエリ実行ごとに呼び出されています。これらのSQLクエリは変更されないため、静的変数にキャッシュすると良いでしょう。

```csharp
private static readonly string MyLocationsSql;
private static readonly string InstanceMembersSql;

static VRCXDatabase()
{
    MyLocationsSql = GetEmbedFileContent("VRCXDiscordTracker.Core.VRCX.Queries.myLocations.sql");
    InstanceMembersSql = GetEmbedFileContent("VRCXDiscordTracker.Core.VRCX.Queries.instanceMembers.sql");
}

public List<MyLocation> GetMyLocations(string vrchatUserId, int locationCount)
{
    // MyLocationsSql を使用
    var sql = MyLocationsSql;
    // ...
}

public List<InstanceMember> GetInstanceMembers(string vrchatUserId, MyLocation myLocation)
{
    // InstanceMembersSql を使用
    var sanitizedUserId = vrchatUserId.Replace("_", "").Replace("-", "");
    var friendTableName = sanitizedUserId + "_friend_log_current";
    var sql = InstanceMembersSql.Replace("@{friendTableName}", friendTableName);
    // ...
}
```

## セキュリティ上の懸念点

1. SQLクエリでパラメータ化を使用していますが、`GetInstanceMembers`メソッドでテーブル名の置換を行っている部分がSQL Injectionのリスクがあります。

```csharp
// 現在のコード
var sanitizedUserId = vrchatUserId.Replace("_", "").Replace("-", "");
var friendTableName = sanitizedUserId + "_friend_log_current";
var sql = GetEmbedFileContent("...").Replace("@{friendTableName}", friendTableName);
```

この部分は、文字列置換でテーブル名を動的に変更していますが、ユーザーIDが悪意あるものだった場合にSQL Injectionの可能性があります。テーブル名のホワイトリスト検証やより厳格なサニタイズを行うべきです。

```csharp
// 改善案
var sanitizedUserId = Regex.Replace(vrchatUserId, "[^a-zA-Z0-9]", ""); // 英数字のみ許可
if (!Regex.IsMatch(sanitizedUserId, "^[a-zA-Z0-9]+$"))
{
    throw new ArgumentException("Invalid user ID format");
}
var friendTableName = sanitizedUserId + "_friend_log_current";
```

2. 例外メッセージにスタックトレースが含まれています。これは開発時には有用ですが、本番環境では内部情報の漏洩リスクとなります。環境に応じてログレベルを調整すべきです。

## 総合評価

VRCXDatabase.csは基本的なデータアクセス機能を提供していますが、リソース管理、例外処理、パフォーマンス最適化、セキュリティの面で改善の余地があります。特に、IDisposableインターフェースの実装、より堅牢な例外処理の実装、SQLインジェクション対策の強化が重要です。また、パフォーマンスを向上させるために、SQLクエリのキャッシュや接続プールの導入を検討すべきです。
