using System.Collections.Concurrent;
using BusinessMapNET.Application.Internal;
using BusinessMapNET.Application.Models;
using BusinessMapNET.Core.Models;
using BusinessMapNET.Core.Services;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Services;

/// <summary>
/// Shared, per-request helper used by the business services. Centralizes cross-cutting concerns
/// such as resolving boards by name or id, parsing and caching board structures, resolving the
/// current user, listing users/boards and translating API failures into clear
/// <see cref="BusinessMapServiceException"/> messages.
/// </summary>
/// <remarks>
/// Registered as a scoped service so the per-request caches (board structures, board list,
/// user list, current user) are shared across the services invoked within a single request but
/// never leak between requests.
/// </remarks>
public sealed class BusinessMapContext
{
    private readonly IBusinessMapClient _client;
    private readonly ILogger<BusinessMapContext> _logger;
    private readonly ConcurrentDictionary<int, Task<BoardStructure>> _structureCache = new();

    private IReadOnlyList<Board>? _cachedBoards;
    private IReadOnlyList<User>? _cachedUsers;
    private int? _currentUserId;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessMapContext"/> class.
    /// </summary>
    public BusinessMapContext(IBusinessMapClient client, ILogger<BusinessMapContext> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>The underlying strongly-typed Businessmap client.</summary>
    public IBusinessMapClient Client => _client;

    /// <summary>
    /// Executes an API call, translating <see cref="Core.Http.BusinessMapApiException"/> into a clear
    /// <see cref="BusinessMapServiceException"/> that describes the failing action.
    /// </summary>
    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, string description) =>
        ApiExecutor.ExecuteAsync(_logger, action, description);

    /// <summary>
    /// Executes an API call that has no return value, translating API failures into
    /// <see cref="BusinessMapServiceException"/>.
    /// </summary>
    public Task ExecuteAsync(Func<Task> action, string description) =>
        ApiExecutor.ExecuteAsync(_logger, action, description);

    /// <summary>
    /// Gets the id of the currently authenticated user, caching the result for the request.
    /// </summary>
    public async Task<int> GetCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        if (_currentUserId is int id)
        {
            return id;
        }

        var me = await ExecuteAsync(
            () => _client.Users.GetCurrentUserAsync(cancellationToken),
            "resolve the current user").ConfigureAwait(false);

        _currentUserId = me.UserId;
        return me.UserId;
    }

    /// <summary>
    /// Gets the list of accessible boards, caching the result for the request.
    /// </summary>
    public async Task<IReadOnlyList<Board>> GetBoardsAsync(CancellationToken cancellationToken)
    {
        _cachedBoards ??= await ExecuteAsync(
            () => _client.Boards.GetBoardsAsync(cancellationToken),
            "list boards").ConfigureAwait(false);

        return _cachedBoards;
    }

    /// <summary>
    /// Gets the list of users in the account, caching the result for the request.
    /// </summary>
    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken)
    {
        _cachedUsers ??= await ExecuteAsync(
            () => _client.Users.GetUsersAsync(cancellationToken),
            "list users").ConfigureAwait(false);

        return _cachedUsers;
    }

    /// <summary>
    /// Resolves a board from either an explicit id or a (case-insensitive) name.
    /// </summary>
    /// <exception cref="BusinessMapServiceException">
    /// Thrown when neither is provided, the id does not exist, the name matches no board, or the name is ambiguous.
    /// </exception>
    public async Task<Board> ResolveBoardAsync(int? boardId, string? boardName, CancellationToken cancellationToken)
    {
        if (boardId is null && string.IsNullOrWhiteSpace(boardName))
        {
            throw new BusinessMapServiceException(
                "You must provide either 'boardId' or 'boardName' to identify the board.");
        }

        var boards = await GetBoardsAsync(cancellationToken).ConfigureAwait(false);

        if (boardId is int id)
        {
            var match = boards.FirstOrDefault(b => b.BoardId == id);
            return match ?? throw new BusinessMapServiceException(
                $"No accessible board was found with id {id}. Use 'list_boards' to see the available boards.");
        }

        var name = boardName!.Trim();
        var byName = boards
            .Where(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (byName.Count == 1)
        {
            return byName[0];
        }

        if (byName.Count == 0)
        {
            var suggestions = boards
                .Where(b => b.Name is not null && b.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(b => $"'{b.Name}' (id {b.BoardId})")
                .ToList();

            var hint = suggestions.Count > 0
                ? " Did you mean: " + string.Join(", ", suggestions) + "?"
                : " Use 'list_boards' to see the available boards.";

            throw new BusinessMapServiceException($"No board named '{name}' was found.{hint}");
        }

        var options = string.Join(", ", byName.Select(b => $"id {b.BoardId}"));
        throw new BusinessMapServiceException(
            $"The board name '{name}' is ambiguous and matches multiple boards ({options}). " +
            "Provide 'boardId' instead to disambiguate.");
    }

    /// <summary>
    /// Gets (and caches) the parsed <see cref="BoardStructure"/> for a board.
    /// </summary>
    public Task<BoardStructure> GetBoardStructureAsync(Board board, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(board);

        return _structureCache.GetOrAdd(board.BoardId, async _ =>
        {
            var raw = await ExecuteAsync(
                () => _client.Boards.GetBoardStructureAsync(board.BoardId, cancellationToken),
                $"read the structure of board {board.BoardId}").ConfigureAwait(false);

            return BoardStructure.Parse(raw, board.BoardId, board.Name);
        });
    }

    /// <summary>
    /// Gets (and caches) the parsed <see cref="BoardStructure"/> for a board id, resolving the board first.
    /// </summary>
    public async Task<BoardStructure> GetBoardStructureAsync(int boardId, CancellationToken cancellationToken)
    {
        var board = await ResolveBoardAsync(boardId, null, cancellationToken).ConfigureAwait(false);
        return await GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);
    }
}
