using BusinessMapNET.Core.DependencyInjection;
using BusinessMapNET.MCPServer.Services;
using BusinessMapNET.MCPServer.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Bind Businessmap (Kanbanize) settings from configuration/environment and register the
// strongly-typed Core client (IBusinessMapClient + resource APIs).
// Configuration precedence: appsettings.json < appsettings.{Environment}.json < environment
// variables (e.g. BusinessMap__ApiKey). Host.CreateApplicationBuilder already loads
// appsettings.Development.json automatically when DOTNET_ENVIRONMENT=Development.
builder.Services.AddBusinessMap(builder.Configuration);

// Shared, per-request helper used by the tools to resolve boards/users and map results.
builder.Services.AddScoped<BusinessMapToolContext>();

// Add the MCP services: the transport to use (stdio) and the high-level Kanbanize tools.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<CardTools>()
    .WithTools<BoardTools>()
    .WithTools<WorkflowTools>()
    .WithTools<TaskTools>()
    .WithTools<CommentTools>()
    .WithTools<UserTools>();

await builder.Build().RunAsync();

