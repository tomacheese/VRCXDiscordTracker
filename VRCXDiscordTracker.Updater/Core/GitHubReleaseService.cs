using System.Text.Json;

namespace VRCXDiscordTracker.Updater.Core;

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
        Console.WriteLine($"GET {url}");
        var uri = new Uri(url);
        var json = await _http.GetStringAsync(uri).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("tag_name", out JsonElement tagNameElement) || tagNameElement.ValueKind != JsonValueKind.String)
        {
            throw new Exception("Failed to get 'tag_name' from GitHub release response.");
        }
        var tagName = tagNameElement.GetString()!;

        if (!root.TryGetProperty("assets", out JsonElement assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Failed to find assets in GitHub API response.");
        }

        JsonElement asset = default;
        foreach (JsonElement item in assetsElement.EnumerateArray())
        {
            if (item.TryGetProperty("name", out JsonElement nameProperty) &&
                nameProperty.ValueKind == JsonValueKind.String &&
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

        if (!asset.TryGetProperty("browser_download_url", out JsonElement browserDownloadUrlElement) ||
            browserDownloadUrlElement.ValueKind != JsonValueKind.String)
        {
            throw new Exception($"Failed to get browser_download_url for asset: {assetName}");
        }
        var assetUrl = browserDownloadUrlElement.GetString()!;

        return new ReleaseInfo(tagName, assetUrl);
    }

    /// <summary>
    /// 指定したURLからファイルをダウンロードして一時ファイルに格納する
    /// </summary>
    /// <param name="url">URL</param>
    /// <returns>一時ファイルのパス</returns>
    public async Task<string> DownloadWithProgressAsync(string url)
    {
        var tmp = Path.GetTempFileName();
        var uri = new Uri(url);
        using HttpResponseMessage res = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        var total = res.Content.Headers.ContentLength ?? -1L;
        using Stream stream = await res.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using FileStream fs = File.OpenWrite(tmp);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
            downloaded += read;
            if (total > 0)
            {
                Console.Write($"\r{downloaded:#,0}/{total:#,0} bytes");
            }
        }
        Console.WriteLine();
        return tmp;
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}
