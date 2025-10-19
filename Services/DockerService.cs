using Docker.DotNet;
using Docker.DotNet.Models;
using OpenHosting.Models;

namespace OpenHosting.Services;

public class DockerService
{
    private readonly DockerClient _dockerClient;
    private readonly ILogger<DockerService> _logger;

    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
        _dockerClient = new DockerClientConfiguration()
            .CreateClient();
    }

    public async Task<List<Container>> GetContainersAsync()
    {
        try
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true });

            return containers.Select(c => new Container
            {
                Id = c.ID,
                Name = c.Names.FirstOrDefault()?.TrimStart('/') ?? c.ID[..12],
                Image = c.Image,
                Status = c.Status,
                State = c.State,
                CreatedAt = c.Created,
                StartedAt = c.Status.StartsWith("Up") ? c.Created : (DateTime?)null,
                Ports = c.Ports.Select(p => new PortMapping
                {
                    HostPort = p.PublicPort == 0 ? 0 : (int)p.PublicPort,
                    ContainerPort = p.PrivatePort == 0 ? 0 : (int)p.PrivatePort,
                    Protocol = p.Type
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get containers");
            return new List<Container>();
        }
    }

    public async Task<Container?> GetContainerAsync(string containerId)
    {
        try
        {
            var container = await _dockerClient.Containers.InspectContainerAsync(containerId);
            return new Container
            {
                Id = container.ID,
                Name = container.Name.TrimStart('/'),
                Image = container.Config.Image,
                Status = container.State.Status,
                State = container.State.Status,
                CreatedAt = container.Created,
                StartedAt = DateTime.TryParse(container.State.StartedAt, out var startedAt) ? startedAt : null,
                Ports = container.NetworkSettings.Ports?.SelectMany(kvp =>
                    kvp.Value?.Select(p => new PortMapping
                    {
                        HostPort = int.Parse(p.HostPort),
                        ContainerPort = int.Parse(kvp.Key.Split('/')[0]),
                        Protocol = kvp.Key.Split('/')[1]
                    }) ?? Enumerable.Empty<PortMapping>()
                ).ToList() ?? new List<PortMapping>(),
                Environment = container.Config.Env?.ToDictionary(
                    e => e.Split('=')[0],
                    e => string.Join("=", e.Split('=').Skip(1))
                ) ?? new Dictionary<string, string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container {ContainerId}", containerId);
            return null;
        }
    }

    public async Task<string> BuildImageAsync(string dockerfilePath, string imageName, string tag = "latest")
    {
        try
        {
            var fullImageName = $"{imageName}:{tag}";
            var buildParameters = new ImageBuildParameters
            {
                Dockerfile = "Dockerfile",
                Tags = new List<string> { fullImageName }
            };

            // TODO: Implement proper tar stream creation for Docker build context
            // For now, we'll use a placeholder approach
            using var buildContext = new MemoryStream();
            using var stream = await _dockerClient.Images.BuildImageFromDockerfileAsync(
                buildContext, buildParameters);

            using var reader = new StreamReader(stream);
            var buildOutput = await reader.ReadToEndAsync();

            _logger.LogInformation("Built image {ImageName}: {Output}", fullImageName, buildOutput);
            return fullImageName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build image {ImageName}", imageName);
            throw;
        }
    }

    public async Task<string> CreateContainerAsync(string imageName, string containerName, 
        List<PortMapping> portMappings, Dictionary<string, string> environment)
    {
        try
        {
            var portBindings = new Dictionary<string, IList<PortBinding>>();
            var exposedPorts = new Dictionary<string, EmptyStruct>();

            foreach (var port in portMappings)
            {
                var key = $"{port.ContainerPort}/{port.Protocol}";
                portBindings[key] = new List<PortBinding>
                {
                    new() { HostPort = port.HostPort.ToString() }
                };
                exposedPorts[key] = new EmptyStruct();
            }

            var createParameters = new CreateContainerParameters
            {
                Image = imageName,
                Name = containerName,
                Env = environment.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList(),
                ExposedPorts = exposedPorts,
                HostConfig = new HostConfig
                {
                    PortBindings = portBindings
                }
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParameters);
            _logger.LogInformation("Created container {ContainerId} with name {ContainerName}", 
                response.ID, containerName);
            
            return response.ID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container {ContainerName}", containerName);
            throw;
        }
    }

    public async Task<bool> StartContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            _logger.LogInformation("Started container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> StopContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
            _logger.LogInformation("Stopped container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> RemoveContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters());
            _logger.LogInformation("Removed container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<string> GetContainerLogsAsync(string containerId, int tail = 100)
    {
        try
        {
            var logs = await _dockerClient.Containers.GetContainerLogsAsync(containerId, new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Tail = tail.ToString()
            });

            using var reader = new StreamReader(logs);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {ContainerId}", containerId);
            return $"Error retrieving logs: {ex.Message}";
        }
    }

    public async Task<List<int>> GetUsedPortsAsync()
    {
        try
        {
            var containers = await GetContainersAsync();
            return containers
                .SelectMany(c => c.Ports)
                .Select(p => p.HostPort)
                .Where(p => p > 0)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get used ports");
            return new List<int>();
        }
    }

    public async Task<int> GetNextAvailablePortAsync(int startPort = 8000)
    {
        var usedPorts = await GetUsedPortsAsync();
        var port = startPort;
        
        while (usedPorts.Contains(port))
        {
            port++;
        }
        
        return port;
    }

    public void Dispose()
    {
        _dockerClient?.Dispose();
    }
}
