using OpenHosting.Models;
using OpenHosting.Services;

namespace OpenHosting.Apps;

[App(icon: Icons.Server, title: "Container Manager")]
public class SimpleContainerApp : ViewBase
{
    private readonly ContainerService _containerService;
    private readonly ILogger<SimpleContainerApp> _logger;

    public SimpleContainerApp(ContainerService containerService, ILogger<SimpleContainerApp> logger)
    {
        _containerService = containerService;
        _logger = logger;
    }

    public override object? Build()
    {
        var containers = this.UseState<List<Container>>(() => new List<Container>());
        var isLoading = this.UseState<bool>(() => false);
        var errorMessage = this.UseState<string?>(() => null);

        // Load data on mount
        this.UseEffect(() =>
        {
            LoadData();
        }, []);

        async Task LoadData()
        {
            try
            {
                isLoading.Value = true;
                errorMessage.Value = null;
                containers.Value = await _containerService.GetAllContainersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load container data");
                errorMessage.Value = $"Failed to load data: {ex.Message}";
            }
            finally
            {
                isLoading.Value = false;
            }
        }

        return Layout.Vertical().Gap(4).Padding(2)
            | Text.H1("OpenHosting - Container Manager")
            | (errorMessage.Value != null ? 
                Text.Block($"Error: {errorMessage.Value}") : null)
            | (isLoading.Value ? 
                Text.Block("Loading containers...") :
                BuildContainerList(containers.Value, LoadData));
    }

    private object BuildContainerList(List<Container> containers, Func<Task> refreshData)
    {
        if (!containers.Any())
        {
            return Layout.Vertical().Gap(4)
                | Text.H3("No containers found")
                | Text.Block("Deploy your first container using the deployment form below")
                | new Button("Refresh", async () => await refreshData());
        }

        return Layout.Vertical().Gap(4)
            | new Button("Refresh", async () => await refreshData())
            | containers.Select(BuildContainerCard).ToArray();
    }

    private object BuildContainerCard(Container container)
    {
        return new Card(
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H4(container.Name)
            | Text.Block($"Image: {container.Image}")
            | Text.Block($"Status: {container.Status}")
            | Text.Block($"Created: {container.CreatedAt:yyyy-MM-dd HH:mm}")
            | (container.Ports.Any() ? 
                Text.Block($"Ports: {string.Join(", ", container.Ports.Select(p => $"{p.HostPort}:{p.ContainerPort}"))}") : 
                null)
            | Layout.Horizontal().Gap(2)
                | new Button("Stop", async () => await StopContainer(container.Id))
                | new Button("Start", async () => await StartContainer(container.Id))
                | new Button("Remove", async () => await RemoveContainer(container.Id))
        );
    }

    private async Task StopContainer(string containerId)
    {
        try
        {
            await _containerService.StopContainerAsync(containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", containerId);
        }
    }

    private async Task StartContainer(string containerId)
    {
        try
        {
            await _containerService.StartContainerAsync(containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container {ContainerId}", containerId);
        }
    }

    private async Task RemoveContainer(string containerId)
    {
        try
        {
            await _containerService.RemoveContainerAsync(containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
        }
    }
}
