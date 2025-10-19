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
        var refreshTrigger = this.UseState<int>(() => 0);
        var containerAction = this.UseState<(string action, string containerId)?>(() => null);

        // Load data on mount and when refresh is triggered
        this.UseEffect(async () =>
        {
            await LoadData();
        }, refreshTrigger);

        // Handle container actions
        this.UseEffect(async () =>
        {
            if (containerAction.Value == null) return;

            var actionData = containerAction.Value.Value;
            var action = actionData.action;
            var containerId = actionData.containerId;
            try
            {
                switch (action)
                {
                    case "stop":
                        await _containerService.StopContainerAsync(containerId);
                        break;
                    case "start":
                        await _containerService.StartContainerAsync(containerId);
                        break;
                    case "remove":
                        await _containerService.RemoveContainerAsync(containerId);
                        break;
                }
                // Refresh the container list after action
                refreshTrigger.Set(refreshTrigger.Value + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to {Action} container {ContainerId}", action, containerId);
                errorMessage.Set($"Failed to {action} container: {ex.Message}");
            }
            finally
            {
                (string action, string containerId)? nullValue = null;
                containerAction.Set(nullValue);
            }
        }, containerAction);

        async Task LoadData()
        {
            try
            {
                isLoading.Set(true);
                errorMessage.Set((string?)null);
                var containerList = await _containerService.GetAllContainersAsync();
                containers.Set(containerList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load container data");
                errorMessage.Set($"Failed to load data: {ex.Message}");
            }
            finally
            {
                isLoading.Set(false);
            }
        }

        return Layout.Vertical().Gap(4).Padding(2)
            | Text.H1("OpenHosting - Container Manager")
            | (errorMessage.Value != null ?
                Text.Block($"Error: {errorMessage.Value}") : null)
            | (isLoading.Value ?
                Text.Block("Loading containers...") :
                BuildContainerList(containers.Value, refreshTrigger, containerAction));
    }

    private object BuildContainerList(List<Container> containers, IState<int> refreshTrigger, IState<(string action, string containerId)?> containerAction)
    {
        if (!containers.Any())
        {
            return Layout.Vertical().Gap(4)
                | Text.H3("No containers found")
                | Text.Block("Deploy your first container using the deployment form below")
                | new Button("Refresh", () => refreshTrigger.Set(refreshTrigger.Value + 1));
        }

        return Layout.Vertical().Gap(4)
            | new Button("Refresh", () => refreshTrigger.Set(refreshTrigger.Value + 1))
            | containers.Select(container => BuildContainerCard(container, containerAction)).ToArray();
    }

    private object BuildContainerCard(Container container, IState<(string action, string containerId)?> containerAction)
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
                | new Button("Stop", () => containerAction.Set(("stop", container.Id)))
                | new Button("Start", () => containerAction.Set(("start", container.Id)))
                | new Button("Remove", () => containerAction.Set(("remove", container.Id)))
        );
    }
}
