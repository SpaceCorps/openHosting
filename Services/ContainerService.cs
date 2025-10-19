using OpenHosting.Models;

namespace OpenHosting.Services;

public class ContainerService
{
    private readonly DockerService _dockerService;
    private readonly GitHubService _gitHubService;
    private readonly ILogger<ContainerService> _logger;
    private readonly List<Deployment> _deployments;

    public ContainerService(DockerService dockerService, GitHubService gitHubService, ILogger<ContainerService> logger)
    {
        _dockerService = dockerService;
        _gitHubService = gitHubService;
        _logger = logger;
        _deployments = new List<Deployment>();
    }

    public async Task<List<Container>> GetAllContainersAsync()
    {
        return await _dockerService.GetContainersAsync();
    }

    public async Task<Container?> GetContainerAsync(string containerId)
    {
        return await _dockerService.GetContainerAsync(containerId);
    }

    public async Task<string> DeployFromGitHubAsync(string repositoryUrl, string name, 
        string branch = "main", Dictionary<string, string>? environment = null)
    {
        try
        {
            _logger.LogInformation("Starting deployment from GitHub repository {RepositoryUrl}", repositoryUrl);

            // Clone repository
            var repositoryPath = await _gitHubService.CloneRepositoryAsync(repositoryUrl, branch);
            
            // Find Dockerfile
            var dockerfilePath = _gitHubService.FindDockerfile(repositoryPath);
            if (dockerfilePath == null)
            {
                throw new InvalidOperationException("No Dockerfile found in repository");
            }

            // Get latest commit
            var commitHash = await _gitHubService.GetLatestCommitAsync(repositoryPath);

            // Build image
            var imageName = $"openhosting-{name.ToLower().Replace(" ", "-")}";
            var imageTag = await _dockerService.BuildImageAsync(
                System.IO.Path.GetDirectoryName(dockerfilePath)!, imageName);

            // Get available port
            var port = await _dockerService.GetNextAvailablePortAsync();
            var portMappings = new List<PortMapping>
            {
                new() { HostPort = port, ContainerPort = 80, Protocol = "tcp" }
            };

            // Create container
            var containerId = await _dockerService.CreateContainerAsync(
                imageTag, name, portMappings, environment ?? new Dictionary<string, string>());

            // Start container
            await _dockerService.StartContainerAsync(containerId);

            // Create deployment record
            var deployment = new Deployment
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                GitHubRepository = repositoryUrl,
                GitHubBranch = branch,
                DockerfilePath = dockerfilePath,
                Environment = environment ?? new Dictionary<string, string>(),
                PortMappings = portMappings,
                AutoDeploy = true,
                CreatedAt = DateTime.UtcNow,
                LastDeployed = DateTime.UtcNow,
                ContainerId = containerId
            };

            _deployments.Add(deployment);

            // Cleanup
            _gitHubService.CleanupRepository(repositoryPath);

            _logger.LogInformation("Successfully deployed {Name} from {RepositoryUrl}", name, repositoryUrl);
            return containerId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy from GitHub repository {RepositoryUrl}", repositoryUrl);
            throw;
        }
    }

    public async Task<bool> RedeployAsync(string deploymentId)
    {
        try
        {
            var deployment = _deployments.FirstOrDefault(d => d.Id == deploymentId);
            if (deployment == null)
            {
                _logger.LogWarning("Deployment {DeploymentId} not found", deploymentId);
                return false;
            }

            _logger.LogInformation("Redeploying {DeploymentName}", deployment.Name);

            // Stop and remove existing container if it exists
            if (!string.IsNullOrEmpty(deployment.ContainerId))
            {
                await _dockerService.StopContainerAsync(deployment.ContainerId);
                await _dockerService.RemoveContainerAsync(deployment.ContainerId);
            }

            // Deploy again
            var newContainerId = await DeployFromGitHubAsync(
                deployment.GitHubRepository,
                deployment.Name,
                deployment.GitHubBranch,
                deployment.Environment);

            // Update deployment record
            deployment.ContainerId = newContainerId;
            deployment.LastDeployed = DateTime.UtcNow;

            _logger.LogInformation("Successfully redeployed {DeploymentName}", deployment.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to redeploy {DeploymentId}", deploymentId);
            return false;
        }
    }

    public async Task<bool> StopContainerAsync(string containerId)
    {
        return await _dockerService.StopContainerAsync(containerId);
    }

    public async Task<bool> StartContainerAsync(string containerId)
    {
        return await _dockerService.StartContainerAsync(containerId);
    }

    public async Task<bool> RemoveContainerAsync(string containerId)
    {
        // Remove from deployments if it exists
        var deployment = _deployments.FirstOrDefault(d => d.ContainerId == containerId);
        if (deployment != null)
        {
            _deployments.Remove(deployment);
        }

        return await _dockerService.RemoveContainerAsync(containerId);
    }

    public async Task<string> GetContainerLogsAsync(string containerId, int tail = 100)
    {
        return await _dockerService.GetContainerLogsAsync(containerId, tail);
    }

    public async Task<List<Deployment>> GetDeploymentsAsync()
    {
        return _deployments.ToList();
    }

    public async Task<Deployment?> GetDeploymentAsync(string deploymentId)
    {
        return _deployments.FirstOrDefault(d => d.Id == deploymentId);
    }

    public async Task<bool> HandleGitHubWebhookAsync(string payload, string signature, string secret)
    {
        try
        {
            // Verify signature
            if (!await _gitHubService.VerifyWebhookSignature(payload, signature, secret))
            {
                _logger.LogWarning("Invalid webhook signature");
                return false;
            }

            // Parse webhook
            var webhook = await _gitHubService.ParseWebhookAsync(payload);
            if (webhook == null)
            {
                _logger.LogWarning("Failed to parse webhook payload");
                return false;
            }

            // Check if it's a main branch push
            if (!_gitHubService.IsMainBranchPush(webhook))
            {
                _logger.LogInformation("Webhook is not for main branch push, ignoring");
                return true;
            }

            // Find deployments for this repository
            var repositoryUrl = webhook.Repository.CloneUrl;
            var affectedDeployments = _deployments.Where(d => 
                d.GitHubRepository == repositoryUrl && d.AutoDeploy).ToList();

            if (!affectedDeployments.Any())
            {
                _logger.LogInformation("No auto-deploy deployments found for repository {Repository}", repositoryUrl);
                return true;
            }

            // Redeploy all affected deployments
            foreach (var deployment in affectedDeployments)
            {
                _logger.LogInformation("Auto-redeploying {DeploymentName} due to push to {Repository}", 
                    deployment.Name, repositoryUrl);
                await RedeployAsync(deployment.Id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle GitHub webhook");
            return false;
        }
    }

    public async Task<List<int>> GetUsedPortsAsync()
    {
        return await _dockerService.GetUsedPortsAsync();
    }

    public async Task<int> GetNextAvailablePortAsync()
    {
        return await _dockerService.GetNextAvailablePortAsync();
    }
}
