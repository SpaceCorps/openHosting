using OpenHosting.Apps;
using OpenHosting.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var builder = Host.CreateApplicationBuilder(args);

// Add logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register services
builder.Services.AddSingleton<DockerService>();
builder.Services.AddSingleton<GitHubService>();
builder.Services.AddSingleton<ContainerService>();

var host = builder.Build();

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

// Register services with Ivy
server.Services.AddSingleton(host.Services.GetRequiredService<DockerService>());
server.Services.AddSingleton(host.Services.GetRequiredService<GitHubService>());
server.Services.AddSingleton(host.Services.GetRequiredService<ContainerService>());

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var chromeSettings = new ChromeSettings().DefaultApp<SimpleContainerApp>().UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);

await server.RunAsync();
