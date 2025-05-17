namespace Updater;

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO.Compression;

internal class Program
{
    private const string GitHubApiUrl = "https://api.github.com/repos/tomacheese/VRCXDiscordTracker/releases/latest";
    private const string UserAgent = "Updater";
    private const string ExpectedExeName = "VRCXDiscordTracker.exe";
    private const string ExpectedZipName = "VRCXDiscordTracker.zip";

    static async Task<int> Main()
    {
        var exePath = ExpectedExeName;
        if (!File.Exists(exePath))
        {
            Console.WriteLine($"Error: File not found: {exePath}");
            return 1;
        }

        var fileVersionInfo = FileVersionInfo.GetVersionInfo(exePath);

        Version localVer;
        if (string.IsNullOrEmpty(fileVersionInfo.FileVersion) || !Version.TryParse(fileVersionInfo.FileVersion, out Version? parsedLocalVer))
        {
            localVer = new Version(0, 0, 0);
        }
        else
        {
            localVer = parsedLocalVer!;
        }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        Console.WriteLine("Checking for updates...");
        string json;
        try
        {
            json = await http.GetStringAsync(GitHubApiUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching release info: {ex.Message}");
            return 1;
        }

        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        var tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
        if (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            tagName = tagName[1..];
        }

        if (!Version.TryParse(tagName, out Version? parsedLatestVer) || parsedLatestVer is null)
        {
            Console.WriteLine($"Invalid version format: {tagName}");
            return 1;
        }
        Version latestVer = parsedLatestVer;

        if (latestVer <= localVer)
        {
            Console.WriteLine($"Current version ({localVer}) is up to date.");
            return 0;
        }

        Console.WriteLine($"Updating from {localVer} to {latestVer}");
        var downloadUrl = (string?)null;
        foreach (JsonElement asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            if (string.Equals(name, ExpectedZipName, StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }
        if (downloadUrl is null)
        {
            Console.WriteLine("Suitable asset not found.");
            return 1;
        }

        Console.WriteLine($"Downloading {downloadUrl}");
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(downloadUrl));
        try
        {
            HttpResponseMessage response = await http.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            using FileStream fs = File.OpenWrite(tempFile);
            await response.Content.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading asset: {ex.Message}");
            return 1;
        }

        var updaterExeName = Path.GetFileName(Environment.ProcessPath ?? exePath); // Updater.exe

        try
        {
            if (Path.GetExtension(downloadUrl).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Extracting zip...");
                using ZipArchive archive = ZipFile.OpenRead(tempFile);
                var targetDir = Path.GetDirectoryName(exePath)!;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Skip the updater itself, extract only the target exe
                    if (string.Equals(entry.Name, updaterExeName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var destinationPath = Path.Combine(targetDir, entry.FullName);
                    Console.WriteLine($"Extracting {entry.FullName} to {destinationPath}");
                    var directory = Path.GetDirectoryName(destinationPath);
                    if (directory is not null && !string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        // Create the directory if it doesn't exist
                        Console.WriteLine($"Creating directory: {directory}");
                        Directory.CreateDirectory(directory);
                    }
                    // Extract the file, overwriting if it exists
                    if (File.Exists(destinationPath))
                    {
                        Console.WriteLine($"Overwriting existing file: {destinationPath}");
                    }
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
            else
            {
                File.Copy(tempFile, exePath, overwrite: true);
            }
            Console.WriteLine("Update applied.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error replacing files: {ex.Message}");
            return 1;
        }
        finally
        {
            File.Delete(tempFile);
        }

        Console.WriteLine("Launching updated application...");
        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true
        });

        return 0;
    }
}