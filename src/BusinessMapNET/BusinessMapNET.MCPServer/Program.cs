using BusinessMapNET.Application.DependencyInjection;
using BusinessMapNET.Core.DependencyInjection;
using BusinessMapNET.MCPServer.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    // Anchor the content root to the executable's directory so appsettings*.json are found
    // regardless of the current working directory (e.g. when launched by an MCP host).
    ContentRootPath = AppContext.BaseDirectory
});

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Bind Businessmap (Kanbanize) settings from configuration/environment and register the
// strongly-typed Core client (IBusinessMapClient + resource APIs).
// Configuration precedence: appsettings.json < appsettings.{Environment}.json < environment
// variables (e.g. BusinessMap__ApiKey). Host.CreateApplicationBuilder already loads
// appsettings.Development.json automatically when DOTNET_ENVIRONMENT=Development.
builder.Services.AddBusinessMap(builder.Configuration);

// Register the business (application) services that encapsulate the logic and internally call
// the Core REST client. The MCP tools stay thin adapters over these services.
builder.Services.AddBusinessMapApplication();

// Add the MCP services: the transport to use (stdio) and the high-level Kanbanize tools.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<BoardTools>()
    .WithTools<CardTools>()
    .WithTools<WorkflowTools>()
    .WithTools<TaskTools>()
    .WithTools<CommentTools>()
    .WithTools<UserTools>();

await builder.Build().RunAsync();

