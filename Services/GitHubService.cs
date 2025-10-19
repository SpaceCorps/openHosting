using OpenHosting.Models;
using System.Text.Json;
using System.Text;
using System.IO;

namespace OpenHosting.Services;

public class GitHubService
{
    private readonly ILogger<GitHubService> _logger;
    private readonly string _tempDirectory;

    public GitHubService(ILogger<GitHubService> logger)
    {
        _logger = logger;
        _tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OpenHosting", "repos");
        Directory.CreateDirectory(_tempDirectory);
    }

    public Task<string> CloneRepositoryAsync(string repositoryUrl, string branch = "main")
    {
        try
        {
            var repoName = ExtractRepositoryName(repositoryUrl);
            var localPath = System.IO.Path.Combine(_tempDirectory, repoName, Guid.NewGuid().ToString());

            _logger.LogInformation("Cloning repository {RepositoryUrl} to {LocalPath}", repositoryUrl, localPath);

            // For now, we'll use a simple approach - in production you'd want to use LibGit2Sharp
            // or implement proper git cloning
            Directory.CreateDirectory(localPath);
            
            // TODO: Implement actual git cloning
            // This is a placeholder - you would use LibGit2Sharp or git command here
            _logger.LogWarning("Git cloning not fully implemented - using placeholder");
            
            _logger.LogInformation("Successfully cloned repository to {LocalPath}", localPath);
            return Task.FromResult(localPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clone repository {RepositoryUrl}", repositoryUrl);
            throw;
        }
    }

    public string? FindDockerfile(string repositoryPath)
    {
        try
        {
            var dockerfilePaths = new[]
            {
                System.IO.Path.Combine(repositoryPath, "Dockerfile"),
                System.IO.Path.Combine(repositoryPath, "dockerfile"),
                System.IO.Path.Combine(repositoryPath, "Dockerfile.dockerfile")
            };

            foreach (var path in dockerfilePaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation("Found Dockerfile at {Path}", path);
                    return path;
                }
            }

            // Search in subdirectories
            var dockerfiles = Directory.GetFiles(repositoryPath, "Dockerfile*", SearchOption.AllDirectories);
            if (dockerfiles.Length > 0)
            {
                _logger.LogInformation("Found Dockerfile at {Path}", dockerfiles[0]);
                return dockerfiles[0];
            }

            _logger.LogWarning("No Dockerfile found in repository {RepositoryPath}", repositoryPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find Dockerfile in {RepositoryPath}", repositoryPath);
            return null;
        }
    }

    public Task<GitHubWebhook?> ParseWebhookAsync(string payload)
    {
        try
        {
            var webhook = JsonSerializer.Deserialize<GitHubWebhook>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Parsed webhook for repository {Repository}", webhook?.Repository?.FullName);
            return Task.FromResult(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse GitHub webhook payload");
            return Task.FromResult<GitHubWebhook?>(null);
        }
    }

    public bool IsMainBranchPush(GitHubWebhook webhook)
    {
        return webhook.Ref == "refs/heads/main" || webhook.Ref == "refs/heads/master";
    }

    public Task<string> GetLatestCommitAsync(string repositoryPath)
    {
        try
        {
            // TODO: Implement actual git commit retrieval
            // This is a placeholder - you would use LibGit2Sharp or git command here
            _logger.LogWarning("Git commit retrieval not fully implemented - using placeholder");
            return Task.FromResult("placeholder-commit-hash");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest commit from {RepositoryPath}", repositoryPath);
            return Task.FromResult(string.Empty);
        }
    }

    public Task<bool> VerifyWebhookSignature(string payload, string signature, string secret)
    {
        try
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = "sha256=" + Convert.ToHexString(computedHash).ToLower();

            return Task.FromResult(computedSignature == signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify webhook signature");
            return Task.FromResult(false);
        }
    }

    public void CleanupRepository(string repositoryPath)
    {
        try
        {
            if (Directory.Exists(repositoryPath))
            {
                Directory.Delete(repositoryPath, true);
                _logger.LogInformation("Cleaned up repository at {RepositoryPath}", repositoryPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup repository {RepositoryPath}", repositoryPath);
        }
    }

    private string ExtractRepositoryName(string repositoryUrl)
    {
        var uri = new Uri(repositoryUrl);
        var path = uri.AbsolutePath.TrimStart('/');
        return path.Replace(".git", "").Replace("/", "-");
    }
}
