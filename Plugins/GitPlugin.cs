using System.ComponentModel;
using System.Diagnostics;
using LibGit2Sharp;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SemanticKernelPlayground.Plugins;

public class GitPlugin
{
    private string _repoPath = string.Empty;
    private readonly string _versionFile = ".release-version";

    [KernelFunction, Description("Sets the Git repository path to work with.")]
    public string SetRepositoryPath(string repositoryPath)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DEBUG] Called SetRepositoryPath with: {repositoryPath}");
        Console.ResetColor();

        if (!Repository.IsValid(repositoryPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Invalid repository path: {repositoryPath}");
            Console.ResetColor();
            return $"Invalid repository path: {repositoryPath}";
        }

        _repoPath = repositoryPath;
        return $"Repository path set to: {repositoryPath}";
    }

    [KernelFunction, Description("Retrieves the latest N commits from a Git repository.")]
    public string GetLatestCommits(int count)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"[DEBUG] Called GetLatestCommits with count: {count}");
        Console.ResetColor();

        if (string.IsNullOrWhiteSpace(_repoPath) || !Repository.IsValid(_repoPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Invalid or unset repository path: {_repoPath}");
            Console.ResetColor();
            return $"Invalid repository path: {_repoPath}";
        }

        using var repo = new Repository(_repoPath);
        var commits = repo.Commits.Take(count)
            .Select(c => $"{c.Id.Sha.Substring(0, 8)} - {c.MessageShort}")
            .ToList();

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"[DEBUG] Found {commits.Count} commits.");
        Console.ResetColor();

        return commits.Count == 0 ? "No commits found." : string.Join("\n", commits);
    }

    [KernelFunction, Description("Retrieves the latest N commits from a GitHub repository URL.")]
    public async Task<string> GetLatestCommitsFromGitHubAsync(string repoUrl, int count)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[DEBUG] Called GetLatestCommitsFromGitHubAsync with URL: {repoUrl}, count: {count}");
        Console.ResetColor();

        var match = System.Text.RegularExpressions.Regex.Match(repoUrl, @"github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)");
        if (!match.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Failed to parse GitHub URL: {repoUrl}");
            Console.ResetColor();
            return $"Invalid GitHub repository URL: {repoUrl}";
        }

        var owner = match.Groups["owner"].Value;
        var repo = match.Groups["repo"].Value;

        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/commits?per_page={count}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SemanticKernelApp");

        var response = await client.GetAsync(apiUrl);
        if (!response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] GitHub API failed: {response.ReasonPhrase}");
            Console.ResetColor();
            return $"Failed to fetch commits: {response.ReasonPhrase}";
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var commits = doc.RootElement.EnumerateArray()
            .Select(e => $"{e.GetProperty("sha").GetString()?[..8]} - {e.GetProperty("commit").GetProperty("message").GetString()?.Split("\\n")[0]}")
            .ToList();

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[DEBUG] Received {commits.Count} commits from GitHub API.");
        Console.ResetColor();

        return commits.Count == 0 ? "No commits found." : string.Join("\n", commits);
    }
    
    [KernelFunction, Description("Applies a semantic version patch (major/minor/patch) and stores it.")]
    public string PatchSemVer(string releaseType)
    {
        var versionPath = Path.Combine(_repoPath, _versionFile);
        var currentVersion = File.Exists(versionPath) ? File.ReadAllText(versionPath).Trim() : "0.0.0";

        if (!Regex.IsMatch(currentVersion, "^\\d+\\.\\d+\\.\\d+$"))
            return "Invalid current version format. Expected MAJOR.MINOR.PATCH";

        var parts = currentVersion.Split('.').Select(int.Parse).ToArray();
        var newVersion = releaseType switch
        {
            "major" => $"{parts[0] + 1}.0.0",
            "minor" => $"{parts[0]}.{parts[1] + 1}.0",
            "patch" => $"{parts[0]}.{parts[1]}.{parts[2] + 1}",
            _ => currentVersion
        };

        File.WriteAllText(versionPath, newVersion);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[DEBUG] Updated semantic version from {currentVersion} to {newVersion}.");
        Console.ResetColor();

        return newVersion;
    }

    [KernelFunction, Description("Gets the latest stored release version.")]
    public string GetLatestStoredVersion()
    {
        var versionPath = Path.Combine(_repoPath, _versionFile);
        if (!File.Exists(versionPath))
            return "0.0.0";

        var version = File.ReadAllText(versionPath).Trim();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[DEBUG] Loaded stored version: {version}");
        Console.ResetColor();
        return version;
    }
    
    [KernelFunction, Description("Commits all changes in the repository with a message.")]
    public string CommitChanges(string message)
    {
        using var repo = new Repository(_repoPath);
        Commands.Stage(repo, "*");

        var author = repo.Config.BuildSignature(DateTimeOffset.Now);
        var commit = repo.Commit(message, author, author);

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"[DEBUG] Created commit: {commit.Sha.Substring(0, 8)} - {commit.MessageShort}");
        Console.ResetColor();

        return $"Commit created: {commit.Sha.Substring(0, 8)} - {commit.MessageShort}";
    }
    
    
    [KernelFunction, Description("Pushes committed changes to the remote using HTTPS and a personal access token provided by the user.")]
    public string PushWithToken()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[INPUT] Enter your GitHub username: ");
            Console.ResetColor();
            var username = Console.ReadLine()?.Trim();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[INPUT] Enter your GitHub Personal Access Token (input hidden recommended): ");
            Console.ResetColor();
            var token = Console.ReadLine()?.Trim();

            using var repo = new Repository(_repoPath);
            var remote = repo.Network.Remotes["origin"];

            var options = new PushOptions
            {
                CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials
                    {
                        Username = username,
                        Password = token
                    }
            };

            repo.Network.Push(remote, "refs/heads/main", options);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[DEBUG] Pushed current branch to remote using HTTPS.");
            Console.ResetColor();

            return "Pushed current branch to remote using HTTPS.";
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Push failed: {ex.Message}");
            Console.ResetColor();
            return $"Push failed: {ex.Message}";
        }
    }
    
    

}
