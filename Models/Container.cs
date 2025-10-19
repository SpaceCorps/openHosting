namespace OpenHosting.Models;

public class Container
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public List<PortMapping> Ports { get; set; } = new();
    public Dictionary<string, string> Environment { get; set; } = new();
    public string? GitHubRepository { get; set; }
    public string? GitHubBranch { get; set; }
    public string? GitHubCommit { get; set; }
    public DateTime? LastDeployed { get; set; }
}

public class PortMapping
{
    public int HostPort { get; set; }
    public int ContainerPort { get; set; }
    public string Protocol { get; set; } = "tcp";
}

public class Deployment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GitHubRepository { get; set; } = string.Empty;
    public string GitHubBranch { get; set; } = "main";
    public string? DockerfilePath { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
    public List<PortMapping> PortMappings { get; set; } = new();
    public bool AutoDeploy { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDeployed { get; set; }
    public string? ContainerId { get; set; }
}

public class GitHubWebhook
{
    public string Ref { get; set; } = string.Empty;
    public Repository Repository { get; set; } = new();
    public List<Commit> Commits { get; set; } = new();
}

public class Repository
{
    public string FullName { get; set; } = string.Empty;
    public string CloneUrl { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
}

public class Commit
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
