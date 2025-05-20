using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VRCXDiscordTracker.Core.Updater;
/// <summary>
/// GitHubのリリース情報を取得するサービス
/// </summary>
internal class GitHubReleaseService
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
        var json = await _http.GetStringAsync(url);
        JObject obj = JsonConvert.DeserializeObject<JObject>(json)!;
        var tagName = obj["tag_name"]!.ToString();
        var assetUrl = obj["assets"]!
            .FirstOrDefault(x => x["name"]!.ToString() == assetName)?["browser_download_url"]?.ToString();
        if (string.IsNullOrEmpty(assetUrl))
        {
            throw new Exception($"Failed to find asset: {assetName}");
        }
        return new ReleaseInfo(tagName, assetUrl);
    }
}