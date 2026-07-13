using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Encapsulates the Businessmap endpoints related to workspaces.
/// </summary>
public interface IWorkspacesApi
{
    /// <summary>
    /// Gets the list of workspaces (<c>GET /workspaces</c>).
    /// </summary>
    Task<IReadOnlyList<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the details of a single workspace (<c>GET /workspaces/{workspace_id}</c>).
    /// </summary>
    Task<Workspace> GetWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a workspace (<c>POST /workspaces</c>).
    /// </summary>
    Task<Workspace> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a workspace (<c>PATCH /workspaces/{workspace_id}</c>).
    /// </summary>
    Task<Workspace> UpdateWorkspaceAsync(int workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken = default);
}
