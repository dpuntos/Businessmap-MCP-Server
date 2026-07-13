using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;
using BusinessMapNET.Core.Services;
using BusinessMapNET.MCPServer.Dtos;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.MCPServer.Services;

/// <summary>
/// Shared helper used by the MCP tool classes. Centralizes cross-cutting concerns such as
/// resolving boards by name or id, parsing and caching board structures, resolving the current
/// user, mapping Businessmap models into serializable DTOs and translating API failures into
/// clear <see cref="ToolException"/> messages.
/// </summary>
/// <remarks>
/// Registered as a scoped service so the per-request caches (board structures, board list,
/// current user) are shared across the tools invoked within a single request but never leak
/// between requests.
/// </remarks>
public sealed class BusinessMapToolContext
{
    private readonly IBusinessMapClient _client;
    private readonly ILogger<BusinessMapToolContext> _logger;
    private readonly ConcurrentDictionary<int, Task<BoardStructure>> _structureCache = new();

    private IReadOnlyList<Board>? _cachedBoards;
    private IReadOnlyList<User>? _cachedUsers;
    private int? _currentUserId;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessMapToolContext"/> class.
    /// </summary>
    public BusinessMapToolContext(IBusinessMapClient client, ILogger<BusinessMapToolContext> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>The underlying strongly-typed Businessmap client.</summary>
    public IBusinessMapClient Client => _client;

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
    /// <param name="boardId">The explicit board id, when known.</param>
    /// <param name="boardName">The board name to resolve when <paramref name="boardId"/> is not provided.</param>
    /// <exception cref="ToolException">
    /// Thrown when neither is provided, the id does not exist, the name matches no board, or the name is ambiguous.
    /// </exception>
    public async Task<Board> ResolveBoardAsync(int? boardId, string? boardName, CancellationToken cancellationToken)
    {
        if (boardId is null && string.IsNullOrWhiteSpace(boardName))
        {
            throw new ToolException("You must provide either 'boardId' or 'boardName' to identify the board.");
        }

        var boards = await GetBoardsAsync(cancellationToken).ConfigureAwait(false);

        if (boardId is int id)
        {
            var match = boards.FirstOrDefault(b => b.BoardId == id);
            return match ?? throw new ToolException(
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

            throw new ToolException($"No board named '{name}' was found.{hint}");
        }

        var options = string.Join(", ", byName.Select(b => $"id {b.BoardId}"));
        throw new ToolException(
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

    /// <summary>
    /// Executes an API call, translating <see cref="BusinessMapApiException"/> into a clear
    /// <see cref="ToolException"/> that describes the failing action.
    /// </summary>
    /// <typeparam name="T">The result type of the API call.</typeparam>
    /// <param name="action">The API call to execute.</param>
    /// <param name="description">A short description of the action, used in error messages and logs.</param>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string description)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (BusinessMapApiException ex)
        {
            _logger.LogError(
                ex,
                "Businessmap API call failed while trying to {Description}. Status: {StatusCode}.",
                description,
                (int)ex.StatusCode);

            throw new ToolException(DescribeApiError(ex, description), ex);
        }
    }

    /// <summary>
    /// Executes an API call that has no return value, translating API failures into <see cref="ToolException"/>.
    /// </summary>
    public async Task ExecuteAsync(Func<Task> action, string description)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            await action().ConfigureAwait(false);
        }
        catch (BusinessMapApiException ex)
        {
            _logger.LogError(
                ex,
                "Businessmap API call failed while trying to {Description}. Status: {StatusCode}.",
                description,
                (int)ex.StatusCode);

            throw new ToolException(DescribeApiError(ex, description), ex);
        }
    }

    /// <summary>Maps a <see cref="Card"/> to a lightweight <see cref="CardSummary"/>.</summary>
    public static CardSummary ToSummary(Card card) => new(
        card.CardId,
        card.CustomId,
        card.Title,
        card.BoardId,
        card.WorkflowId,
        card.ColumnId,
        card.LaneId,
        card.Position,
        card.Priority,
        card.OwnerUserId,
        card.Deadline,
        card.IsBlocked == 1,
        card.Size,
        card.TypeId,
        card.TagIds);

    /// <summary>Maps a <see cref="CardComment"/> to a <see cref="CommentInfo"/>.</summary>
    public static CommentInfo ToInfo(CardComment comment) => new(
        comment.CommentId,
        comment.Type,
        comment.Text,
        comment.CreatedAt,
        comment.LastModified);

    /// <summary>Maps a <see cref="CardSubtask"/> to a <see cref="SubtaskInfo"/>.</summary>
    public static SubtaskInfo ToInfo(CardSubtask subtask) => new(
        subtask.SubtaskId,
        subtask.Description,
        !string.IsNullOrEmpty(subtask.FinishedAt),
        subtask.OwnerUserId,
        subtask.Deadline,
        subtask.FinishedAt,
        subtask.Position);

    /// <summary>
    /// Extracts custom field values from a card's raw <c>custom_fields</c> payload into a flat list.
    /// Tolerates both array and object-keyed shapes.
    /// </summary>
    public static IReadOnlyList<CustomFieldValueInfo> ReadCustomFields(JsonElement? customFields)
    {
        var results = new List<CustomFieldValueInfo>();
        if (customFields is not { } element)
        {
            return results;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (TryReadCustomField(item, out var info))
                    {
                        results.Add(info);
                    }
                }
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object &&
                        TryReadCustomField(property.Value, out var info))
                    {
                        results.Add(info);
                    }
                }
                break;
        }

        return results;
    }

    private static bool TryReadCustomField(JsonElement element, out CustomFieldValueInfo info)
    {
        info = default!;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("field_id", out var fieldIdElement) ||
            !fieldIdElement.TryGetInt32(out var fieldId))
        {
            return false;
        }

        string? value = null;
        if (element.TryGetProperty("value", out var valueElement))
        {
            value = valueElement.ValueKind switch
            {
                JsonValueKind.String => valueElement.GetString(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => valueElement.ToString()
            };
        }

        info = new CustomFieldValueInfo(fieldId, value);
        return true;
    }

    private static string DescribeApiError(BusinessMapApiException ex, string description)
    {
        var code = (int)ex.StatusCode;
        var reason = code switch
        {
            401 or 403 => "the API key is missing or lacks permission for this operation",
            404 => "the requested resource was not found",
            422 => "the request was rejected as invalid (check the provided ids and values)",
            429 => "the API rate limit was exceeded; please retry shortly",
            >= 500 => "the Businessmap service reported a server error; please retry shortly",
            _ => "the Businessmap API returned an error"
        };

        var detail = ExtractApiMessage(ex.ResponseBody);
        var suffix = detail is null ? string.Empty : $" Details: {detail}";
        return $"Could not {description} because {reason} (HTTP {code}).{suffix}";
    }

    private static string? ExtractApiMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }

                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var nestedMessage) &&
                    nestedMessage.ValueKind == JsonValueKind.String)
                {
                    return nestedMessage.GetString();
                }
            }

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // The body was not JSON; fall through and return a trimmed snippet.
        }

        var trimmed = responseBody.Trim();
        return trimmed.Length > 300 ? trimmed[..300] + "…" : trimmed;
    }

    /// <summary>
    /// Parses an ISO 8601 date string into a <see cref="DateTimeOffset"/>, returning
    /// <see langword="null"/> when parsing fails or the input is empty.
    /// </summary>
    public static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;

    /// <summary>Maps a <see cref="User"/> to a lightweight <see cref="UserSummary"/>.</summary>
    public static UserSummary ToSummary(User user) => new(
        user.UserId,
        user.RealName,
        user.Username,
        user.Email,
        user.IsEnabled == 1);
}
