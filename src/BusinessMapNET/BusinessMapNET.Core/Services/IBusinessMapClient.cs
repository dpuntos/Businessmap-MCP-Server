namespace BusinessMapNET.Core.Services;

/// <summary>
/// A facade that exposes the different Businessmap (Kanbanize) resource clients through a single entry point.
/// </summary>
public interface IBusinessMapClient
{
    /// <summary>Endpoints related to the current user.</summary>
    IUsersApi Users { get; }

    /// <summary>Endpoints related to workspaces.</summary>
    IWorkspacesApi Workspaces { get; }

    /// <summary>Endpoints related to boards.</summary>
    IBoardsApi Boards { get; }

    /// <summary>Endpoints related to cards (including comments and subtasks).</summary>
    ICardsApi Cards { get; }
}
