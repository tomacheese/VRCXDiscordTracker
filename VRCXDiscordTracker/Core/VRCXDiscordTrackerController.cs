using System.Diagnostics;
using VRCXDiscordTracker.Core.Notification;
using VRCXDiscordTracker.Core.VRCX;
using Timer = System.Windows.Forms.Timer;

namespace VRCXDiscordTracker.Core;
internal class VRCXDiscordTrackerController
{
    private readonly string _databasePath;
    private readonly VRCXDatabase _vrcxDatabase;
    private readonly Timer _timer = new()
    {
        Interval = 5000,
        Enabled = false,
    };

    public VRCXDiscordTrackerController(string databasePath)
    {
        // データベースパスが指定されていない場合は、デフォルトのVRCXデータベースパスを使用する
        string defaultLogPath = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VRCX",
            "VRCX.sqlite3"
        ));
        _databasePath = databasePath ?? defaultLogPath;
        if (string.IsNullOrEmpty(_databasePath))
        {
            _databasePath = defaultLogPath;
        }

        _vrcxDatabase = new VRCXDatabase(_databasePath);

        _timer.Tick += OnTimerTick;
    }

    public void Start()
    {
        _vrcxDatabase.Open();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _vrcxDatabase.Close();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _vrcxDatabase.Close();
        _vrcxDatabase.Dispose();
    }

    public string GetDatabasePath()
    {
        return _databasePath;
    }

    public string GetVRChatUserId()
    {
        return _vrcxDatabase.GetVRChatUserId();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        Task.Run(Run).Wait();
    }

    private async Task Run()
    {
        var userId = _vrcxDatabase.GetVRChatUserId();
        Debug.WriteLine($"GetVRChatUserId: {userId}");

        var myLocations = _vrcxDatabase.GetMyLocations(userId);
        Debug.WriteLine($"GetMyLocations: {myLocations.Count}");

        foreach (var myLocation in myLocations)
        {
            var instanceMembers = _vrcxDatabase.GetInstanceMembers(userId, myLocation.Location, myLocation.JoinCreatedAt, myLocation.EstimatedLeaveCreatedAt);
            Console.WriteLine($"GetInstanceMembers: {instanceMembers.Count}");

            await new DiscordNotificationService(myLocation, instanceMembers).SendUpdateMessageAsync();
        }
    }
}
