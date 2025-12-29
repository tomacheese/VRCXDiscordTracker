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

        if (!root.TryGetProperty("tag_name", out var tagNameElement) || tagNameElement.ValueKind != JsonValueKind.String)
        {
            throw new Exception("Failed to get 'tag_name' from GitHub release response.");
        }
        var tagName = tagNameElement.GetString()!;

        if (!root.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Failed to find assets in GitHub API response.");
        }

        JsonElement asset = default;
        foreach (var item in assetsElement.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProperty) &&
                nameProperty.GetString() == assetName)
            {
                asset = item;
                break;
            }
        }

        if (asset.ValueKind == JsonValueKind.Undefined)
        {
            throw new Exception($"Failed to find asset: {assetName}");
        }

        if (!asset.TryGetProperty("browser_download_url", out var browserDownloadUrlElement) ||
            browserDownloadUrlElement.ValueKind != JsonValueKind.String)
        {
            throw new Exception($"Failed to get browser_download_url for asset: {assetName}");
        }
        var assetUrl = browserDownloadUrlElement.GetString()!;

        return new ReleaseInfo(tagName, assetUrl);
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}
