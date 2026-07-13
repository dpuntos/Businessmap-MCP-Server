using BusinessMapNET.Application.Models;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Application.Services;

/// <summary>Business operations to discover boards and summarize their operational status.</summary>
public interface IBoardService
{
    /// <summary>Lists the accessible boards, optionally filtered by name and archived state.</summary>
    Task<IReadOnlyList<Board>> ListBoardsAsync(
        string? nameContains,
        bool includeArchived,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves the workspace names keyed by workspace id, or <see langword="null"/> on failure.</summary>
    Task<IReadOnlyDictionary<int, string?>?> TryGetWorkspaceNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>Produces an operational snapshot of a board (by id or name).</summary>
    Task<BoardStatusReport> GetBoardStatusAsync(
        int? boardId,
        string? boardName,
        CancellationToken cancellationToken = default);
}
