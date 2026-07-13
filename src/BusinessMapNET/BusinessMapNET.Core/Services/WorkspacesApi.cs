using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Default implementation of <see cref="IWorkspacesApi"/>.
/// </summary>
public sealed class WorkspacesApi : BusinessMapApiClient, IWorkspacesApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspacesApi"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    public WorkspacesApi(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<Workspace>>("workspaces", cancellationToken);

    /// <inheritdoc />
    public Task<Workspace> GetWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default) =>
        GetAsync<Workspace>(
            FormattableString.Invariant($"workspaces/{workspaceId}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<Workspace> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<Workspace>("workspaces", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Workspace> UpdateWorkspaceAsync(int workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PatchAsync<Workspace>(
            FormattableString.Invariant($"workspaces/{workspaceId}"),
            request,
            cancellationToken);
    }
}
