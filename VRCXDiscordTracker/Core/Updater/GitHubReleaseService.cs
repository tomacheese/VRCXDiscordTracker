using System.Text.Json;

namespace VRCXDiscordTracker.Core.Updater;

/// <summary>
/// GitHubのリリース情報を取得するサービス
/// </summary>
internal class GitHubReleaseService : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;

    /// <summary>
    /// GitHubのリポジトリ情報を指定してインスタンスを生成する
    /// </summary>
    /// <param name="owner">リポジトリオーナー</param>
    /// <param name="repo">リポジトリ名</param>
    public GitHubReleaseService(string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
        _http = new HttpClient();
        var userAgent = $"{owner} {repo} ({AppConstants.AppVersionString})";
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    }

    /// <summary>
    /// 最新リリースの情報を取得する
    /// </summary>
    /// <param name="assetName">アセット名</param>
    /// <returns>リリース情報</returns>
    /// <exception cref="Exception">アセットが見つからない場合</exception>
    public async Task<ReleaseInfo> GetLatestReleaseAsync(string assetName)
    {
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
        var uri = new Uri(url);
        var json = await _http.GetStringAsync(uri).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var tagName = root.GetProperty("tag_name").GetString()!;
        var asset = root.GetProperty("assets").EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("name").GetString() == assetName);
        if (asset.ValueKind == JsonValueKind.Undefined)
        {
            throw new Exception($"Failed to find asset: {assetName}");
        }
        var assetUrl = asset.GetProperty("browser_download_url").GetString()!;
        return new ReleaseInfo(tagName, assetUrl);
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}
