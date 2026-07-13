using System.Text.Json;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Encapsulates the Businessmap endpoints related to boards.
/// </summary>
public interface IBoardsApi
{
    /// <summary>
    /// Gets the list of boards (<c>GET /boards</c>).
    /// </summary>
    Task<IReadOnlyList<Board>> GetBoardsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the details of a single board (<c>GET /boards/{board_id}</c>).
    /// </summary>
    Task<Board> GetBoardAsync(int boardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a board (<c>POST /boards</c>).
    /// </summary>
    Task<Board> CreateBoardAsync(CreateBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a board (<c>PATCH /boards/{board_id}</c>).
    /// </summary>
    Task<Board> UpdateBoardAsync(int boardId, UpdateBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a board (<c>DELETE /boards/{board_id}</c>).
    /// </summary>
    Task DeleteBoardAsync(int boardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current structure (columns, lanes, workflows) of a board
    /// (<c>GET /boards/{board_id}/currentStructure</c>) as raw JSON.
    /// </summary>
    Task<JsonElement> GetBoardStructureAsync(int boardId, CancellationToken cancellationToken = default);
}
