using BusinessMapNET.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessMapNET.Application.DependencyInjection;

/// <summary>
/// Extension methods to register the Businessmap business (application) services with an
/// <see cref="IServiceCollection"/>. These services encapsulate the business logic and internally
/// call the Core REST client, so hosts (such as the MCP server) can stay thin.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared per-request context and the business services. The caller is expected
    /// to have already registered the Core client via <c>AddBusinessMap(...)</c>.
    /// </summary>
    public static IServiceCollection AddBusinessMapApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped so the per-request caches on BusinessMapContext are shared across the services
        // invoked within a single request but never leak between requests.
        services.AddScoped<BusinessMapContext>();

        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWorkflowService, WorkflowService>();

        return services;
    }
}
