using System.Diagnostics;
using VRCXDiscordTracker.Core.Notification;
using VRCXDiscordTracker.Core.VRCX;
using Timer = System.Windows.Forms.Timer;

namespace VRCXDiscordTracker.Core;

/// <summary>
/// VRCXDiscordTrackerのコントローラークラス
/// </summary>
internal class VRCXDiscordTrackerController
{
    /// <summary>
    /// VRCXのSQLiteデータベースのパス
    /// </summary>
    private readonly string _databasePath;

    /// <summary>
    /// VRCXのSQLiteデータベースのインスタンス
    /// </summary>
    private readonly VRCXDatabase _vrcxDatabase;

    /// <summary>
    /// 定期的に監視処理を行うためのタイマー
    /// </summary>
    private readonly Timer _timer = new()
    {
        Interval = 5000, // per 5 seconds
        Enabled = false, // Start() しない限りは動作させない
    };

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="databasePath">VRCXのSQLiteデータベースのパス</param>
    public VRCXDiscordTrackerController(string databasePath)
    {
        // データベースパスが指定されていない場合は、デフォルトのVRCXデータベースパスを使用する
        var defaultLogPath = AppConstants.VRCXDefaultDatabasePath;
        _databasePath = databasePath ?? defaultLogPath;
        if (string.IsNullOrEmpty(_databasePath))
        {
            _databasePath = defaultLogPath;
        }

        _vrcxDatabase = new VRCXDatabase(_databasePath);

        _timer.Tick += OnTimerTick;
    }

    /// <summary>
    /// 監視を開始する
    /// </summary>
    public void Start()
    {
        Console.WriteLine("VRCXDiscordTrackerController.Start()");
        _vrcxDatabase.Open();
        _timer.Start();
    }

    /// <summary>
    /// 監視を破棄する
    /// </summary>
    public void Dispose()
    {
        Console.WriteLine("VRCXDiscordTrackerController.Dispose()");
        _timer.Stop();
        _timer.Dispose();
        _vrcxDatabase.Close();
        _vrcxDatabase.Dispose();
    }

    /// <summary>
    /// VRCXのSQLiteデータベースのパスを取得する
    /// </summary>
    /// <returns></returns>
    public string GetDatabasePath() => _databasePath;

    /// <summary>
    /// VRChatのユーザーIDを取得する
    /// </summary>
    /// <returns></returns>
    public string GetVRChatUserId() => _vrcxDatabase.GetVRChatUserId();

    /// <summary>
    /// 監視処理を実行する
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">EventArgs</param>
    private void OnTimerTick(object? sender, EventArgs e) => Task.Run(Run).Wait();

    /// <summary>
    /// 監視処理
    /// </summary>
    /// <returns>Task</returns>
    private async Task Run()
    {
        Console.WriteLine("VRCXDiscordTrackerController.Run()");
        var userId = _vrcxDatabase.GetVRChatUserId();
        Debug.WriteLine($"GetVRChatUserId: {userId}");

        List<MyLocation> myLocations = _vrcxDatabase.GetMyLocations(userId);
        Debug.WriteLine($"GetMyLocations: {myLocations.Count}");

        foreach (MyLocation myLocation in myLocations)
        {
            List<InstanceMember> instanceMembers = _vrcxDatabase.GetInstanceMembers(userId, myLocation.Location, myLocation.JoinCreatedAt, myLocation.EstimatedLeaveCreatedAt);
            Console.WriteLine($"GetInstanceMembers: {instanceMembers.Count}");

            await new DiscordNotificationService(myLocation, instanceMembers).SendUpdateMessageAsync();
        }
    }
}