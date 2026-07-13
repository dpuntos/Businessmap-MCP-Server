using System.Text.Json;
using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Default implementation of <see cref="IBoardsApi"/>.
/// </summary>
public sealed class BoardsApi : BusinessMapApiClient, IBoardsApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoardsApi"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    public BoardsApi(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Board>> GetBoardsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<Board>>("boards", cancellationToken);

    /// <inheritdoc />
    public Task<Board> GetBoardAsync(int boardId, CancellationToken cancellationToken = default) =>
        GetAsync<Board>(
            FormattableString.Invariant($"boards/{boardId}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<Board> CreateBoardAsync(CreateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<Board>("boards", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Board> UpdateBoardAsync(int boardId, UpdateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PatchAsync<Board>(
            FormattableString.Invariant($"boards/{boardId}"),
            request,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteBoardAsync(int boardId, CancellationToken cancellationToken = default) =>
        DeleteAsync(
            FormattableString.Invariant($"boards/{boardId}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<JsonElement> GetBoardStructureAsync(int boardId, CancellationToken cancellationToken = default) =>
        GetAsync<JsonElement>(
            FormattableString.Invariant($"boards/{boardId}/currentStructure"),
            cancellationToken);
}
