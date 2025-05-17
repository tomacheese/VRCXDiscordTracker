namespace VRCXDiscordTracker.Updater.Core;

/// <summary>
/// GitHubのリリース情報
/// </summary>
/// <param name="tagName">タグ名</param>
/// <param name="assetUrl">アセット URL</param>
internal class ReleaseInfo(string tagName, string assetUrl)
{
    /// <summary>
    /// リリースのタグ名
    /// </summary>
    public SemanticVersion Version { get; } = SemanticVersion.Parse(tagName.TrimStart('v'));

    /// <summary>
    /// アセットのURL
    /// </summary>
    public string AssetUrl { get; } = assetUrl;
}