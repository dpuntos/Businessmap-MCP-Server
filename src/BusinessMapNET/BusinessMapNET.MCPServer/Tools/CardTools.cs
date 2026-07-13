using System.ComponentModel;
using BusinessMapNET.Core.Models;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to search, inspect and manipulate Kanbanize (Businessmap) cards.
/// These tools resolve boards/columns/lanes by name where reasonable and validate destinations,
/// so an agent can work in terms of user intent rather than internal ids.
/// </summary>
[McpServerToolType]
public sealed class CardTools
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;
    private const int MaxScanCards = 500;

    private readonly BusinessMapToolContext _context;
    private readonly ILogger<CardTools> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardTools"/> class.
    /// </summary>
    public CardTools(BusinessMapToolContext context, ILogger<CardTools> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches for cards using any combination of filters. Server-side filters (board, workflow,
    /// column, lane, assignee, type, priority, tags, state, blocked) are pushed to the API; free-text,
    /// deadline range and custom-field filters are applied locally over the matched cards.
    /// </summary>
    /// <remarks>
    /// Example: find active, blocked cards assigned to the current user on the "Delivery" board:
    /// <code>
    /// find_cards(boardName: "Delivery", assignedToMe: true, isBlocked: true, state: "active")
    /// </code>
    /// Example: find cards mentioning "login" due before 2025-01-31:
    /// <code>
    /// find_cards(text: "login", deadlineBefore: "2025-01-31")
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "find_cards")]
    [Description(
        "Search Kanbanize cards using combinable filters: free text (title/description/custom id), board " +
        "(by id or name), assignee (owner id or the current user), workflow, column, lane, tags, card type, " +
        "priority, lifecycle state, blocked flag and deadline range. Returns a paginated list of card summaries. " +
        "Use this to locate cards before acting on them.")]
    public async Task<FindCardsResult> FindCardsAsync(
        [Description("Free text to match against the card title, description or custom id (case-insensitive).")]
        string? text = null,
        [Description("Restrict to a board by its numeric id. Prefer this over 'boardName' when known.")]
        int? boardId = null,
        [Description("Restrict to a board by its name (case-insensitive). Ignored when 'boardId' is set.")]
        string? boardName = null,
        [Description("Restrict to cards owned by (assigned to) this user id.")]
        int? assigneeUserId = null,
        [Description("When true, restrict to cards owned by the currently authenticated user.")]
        bool assignedToMe = false,
        [Description("Restrict to cards in these workflow ids.")]
        int[]? workflowIds = null,
        [Description("Restrict to cards in these column ids.")]
        int[]? columnIds = null,
        [Description("Restrict to cards in these lane ids.")]
        int[]? laneIds = null,
        [Description("Restrict to cards tagged with these tag ids.")]
        int[]? tagIds = null,
        [Description("Restrict to cards of these card type ids.")]
        int[]? typeIds = null,
        [Description("Restrict to cards with these numeric priorities.")]
        int[]? priorities = null,
        [Description("Lifecycle state: 'active' (default), 'archived' or 'discarded'.")]
        string? state = null,
        [Description("Restrict to blocked cards (true) or unblocked cards (false).")]
        bool? isBlocked = null,
        [Description("Only include cards with a deadline on or after this ISO 8601 date/time.")]
        string? deadlineAfter = null,
        [Description("Only include cards with a deadline on or before this ISO 8601 date/time.")]
        string? deadlineBefore = null,
        [Description("1-based page number to return. Defaults to 1.")]
        int page = 1,
        [Description("Maximum cards per page (1-100). Defaults to 25.")]
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ToolException("'page' must be greater than or equal to 1.");
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var stateFilter = ParseState(state);
        var deadlineFrom = RequireValidDate(deadlineAfter, nameof(deadlineAfter));
        var deadlineTo = RequireValidDate(deadlineBefore, nameof(deadlineBefore));

        var notes = new List<string>();

        int? resolvedBoardId = boardId;
        if (resolvedBoardId is null && !string.IsNullOrWhiteSpace(boardName))
        {
            var board = await _context.ResolveBoardAsync(null, boardName, cancellationToken).ConfigureAwait(false);
            resolvedBoardId = board.BoardId;
            notes.Add($"Resolved board '{boardName}' to id {board.BoardId}.");
        }

        int? ownerFilter = assigneeUserId;
        if (assignedToMe)
        {
            var me = await _context.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
            if (ownerFilter is not null && ownerFilter != me)
            {
                throw new ToolException(
                    "Specify either 'assigneeUserId' or 'assignedToMe', not both with different users.");
            }

            ownerFilter = me;
        }

        var hasLocalFilters =
            !string.IsNullOrWhiteSpace(text) || deadlineFrom is not null || deadlineTo is not null;

        _logger.LogInformation(
            "find_cards board={BoardId} owner={Owner} localFilters={HasLocal} page={Page} pageSize={PageSize}",
            resolvedBoardId,
            ownerFilter,
            hasLocalFilters,
            page,
            pageSize);

        return hasLocalFilters
            ? await SearchWithLocalFiltersAsync(
                text, resolvedBoardId, ownerFilter, workflowIds, columnIds, laneIds, tagIds, typeIds,
                priorities, stateFilter, isBlocked, deadlineFrom, deadlineTo, page, pageSize, notes, cancellationToken)
                .ConfigureAwait(false)
            : await SearchServerPagedAsync(
                resolvedBoardId, ownerFilter, workflowIds, columnIds, laneIds, tagIds, typeIds,
                priorities, stateFilter, isBlocked, page, pageSize, notes, cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the full detail of a card: basic data, board name, assignees, tags, comments,
    /// subtasks and custom fields. Comments and subtasks are fetched in parallel with the card.
    /// </summary>
    /// <remarks>
    /// Example: <c>get_card_details(cardId: 12345)</c>.
    /// </remarks>
    [McpServerTool(Name = "get_card_details")]
    [Description(
        "Get the complete detail of a single card by id: basic fields, board, workflow, assignee and co-owners, " +
        "tags, comments, subtasks (checklist) and custom fields. Use this to gather full context about one card " +
        "in a single call before answering questions or acting on it.")]
    public async Task<CardDetails> GetCardDetailsAsync(
        [Description("The numeric id of the card to inspect.")]
        int cardId,
        [Description("Whether to include comments. Defaults to true.")]
        bool includeComments = true,
        [Description("Whether to include subtasks (checklist items). Defaults to true.")]
        bool includeSubtasks = true,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new ToolException("'cardId' must be a positive card id.");
        }

        var cardTask = _context.ExecuteAsync(
            () => _context.Client.Cards.GetCardAsync(cardId, cancellationToken),
            $"read card {cardId}");

        var commentsTask = includeComments
            ? _context.ExecuteAsync(
                () => _context.Client.Cards.GetCardCommentsAsync(cardId, cancellationToken),
                $"read comments of card {cardId}")
            : Task.FromResult<IReadOnlyList<CardComment>>([]);

        var subtasksTask = includeSubtasks
            ? _context.ExecuteAsync(
                () => _context.Client.Cards.GetCardSubtasksAsync(cardId, cancellationToken),
                $"read subtasks of card {cardId}")
            : Task.FromResult<IReadOnlyList<CardSubtask>>([]);

        await Task.WhenAll(cardTask, commentsTask, subtasksTask).ConfigureAwait(false);

        var card = await cardTask.ConfigureAwait(false);
        var comments = await commentsTask.ConfigureAwait(false);
        var subtasks = await subtasksTask.ConfigureAwait(false);

        string? boardName = null;
        try
        {
            var boards = await _context.GetBoardsAsync(cancellationToken).ConfigureAwait(false);
            boardName = boards.FirstOrDefault(b => b.BoardId == card.BoardId)?.Name;
        }
        catch (ToolException ex)
        {
            _logger.LogWarning(ex, "Could not resolve board name for card {CardId}.", cardId);
        }

        return new CardDetails(
            card.CardId,
            card.CustomId,
            card.Title,
            card.Description,
            card.BoardId,
            boardName,
            card.WorkflowId,
            card.ColumnId,
            card.LaneId,
            card.Position,
            card.Priority,
            card.OwnerUserId,
            card.CoOwnerIds,
            card.WatcherIds,
            card.TypeId,
            card.Size,
            card.Color,
            card.Deadline,
            card.IsBlocked == 1,
            card.TagIds,
            card.CreatedAt,
            card.LastModified,
            comments.Select(BusinessMapToolContext.ToInfo).ToList(),
            subtasks.Select(BusinessMapToolContext.ToInfo).ToList(),
            BusinessMapToolContext.ReadCustomFields(card.CustomFields));
    }

    /// <summary>
    /// Creates a card on a board, resolving the target column and lane by id or name and validating
    /// that they exist on the board before creating.
    /// </summary>
    /// <remarks>
    /// Example: create a card in the "Backlog" column of the "Delivery" board and assign it to the current user:
    /// <code>
    /// create_card(title: "Fix login", boardName: "Delivery", columnName: "Backlog", assignToMe: true)
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "create_card")]
    [Description(
        "Create a new card on a board. The target board can be given by id or name; the initial column and lane " +
        "can be given by id or name and are validated against the board structure. Optionally set description, " +
        "assignee (owner), tags, priority, size, card type, color, deadline and custom fields. Returns the created card.")]
    public async Task<CardActionResult> CreateCardAsync(
        [Description("The title of the new card. Required and non-empty.")]
        string title,
        [Description("Target board id. Provide this or 'boardName'.")]
        int? boardId = null,
        [Description("Target board name (case-insensitive). Provide this or 'boardId'.")]
        string? boardName = null,
        [Description("Target column id. Provide this or 'columnName'.")]
        int? columnId = null,
        [Description("Target column name (case-insensitive). Provide this or 'columnId'.")]
        string? columnName = null,
        [Description("Target lane id. Provide this or 'laneName'. When omitted, the board default is used.")]
        int? laneId = null,
        [Description("Target lane name (case-insensitive). Provide this or 'laneId'.")]
        string? laneName = null,
        [Description("The description/body of the card.")]
        string? description = null,
        [Description("The owner (assignee) user id.")]
        int? assigneeUserId = null,
        [Description("When true, assign the new card to the currently authenticated user.")]
        bool assignToMe = false,
        [Description("Tag ids to attach to the card.")]
        int[]? tagIds = null,
        [Description("Numeric priority to set on the card.")]
        int? priority = null,
        [Description("Size/estimate to set on the card.")]
        decimal? size = null,
        [Description("Card type id. Validated against the board when provided.")]
        int? typeId = null,
        [Description("Card color as a hex string (for example 'ff0000').")]
        string? color = null,
        [Description("Deadline in ISO 8601 format (for example '2025-01-31' or '2025-01-31T17:00:00Z').")]
        string? deadline = null,
        [Description("Custom field values to set, as pairs of field id and text value.")]
        CustomFieldValueInfo[]? customFields = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ToolException("'title' is required and cannot be empty.");
        }

        var deadlineValue = NormalizeDeadline(deadline);
        var board = await _context.ResolveBoardAsync(boardId, boardName, cancellationToken).ConfigureAwait(false);
        var structure = await _context.GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);

        var resolvedColumnId = ResolveColumn(structure, columnId, columnName, required: true);
        var resolvedLaneId = ResolveLane(structure, laneId, laneName, required: false);

        if (typeId is int type && !structure.CardTypeExists(type))
        {
            throw new ToolException(
                $"Card type id {type} does not exist on board '{board.Name}' (id {board.BoardId}). " +
                "Use 'get_workflow' to see the valid card types.");
        }

        var ownerId = await ResolveOwnerAsync(assigneeUserId, assignToMe, cancellationToken).ConfigureAwait(false);

        var request = new CreateCardRequest
        {
            Title = title.Trim(),
            ColumnId = resolvedColumnId,
            LaneId = resolvedLaneId,
            Description = description,
            OwnerUserId = ownerId,
            Priority = priority,
            Size = size,
            TypeId = typeId,
            Color = color,
            Deadline = deadlineValue,
            TagIdsToAdd = tagIds is { Length: > 0 } ? tagIds : null,
            CustomFieldsToAddOrUpdate = MapCustomFields(customFields),
        };

        _logger.LogInformation(
            "create_card board={BoardId} column={ColumnId} lane={LaneId}",
            board.BoardId,
            resolvedColumnId,
            resolvedLaneId);

        var created = await _context.ExecuteAsync(
            () => _context.Client.Cards.CreateCardAsync(request, cancellationToken),
            $"create a card on board {board.BoardId}").ConfigureAwait(false);

        return new CardActionResult(
            created.CardId,
            created.Title,
            created.BoardId,
            created.ColumnId,
            created.LaneId,
            $"Created card {created.CardId} on board '{board.Name}' (id {board.BoardId}).");
    }

    /// <summary>
    /// Updates the content fields of a card without requiring the full object. Only the provided
    /// fields are changed; the rest are left untouched.
    /// </summary>
    /// <remarks>
    /// Example: raise priority and set a deadline:
    /// <code>
    /// update_card(cardId: 12345, priority: 3, deadline: "2025-02-15")
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "update_card")]
    [Description(
        "Update selected fields of an existing card without sending the whole object. Supports title, description, " +
        "priority, size, card type, color, deadline, blocked flag, adding/removing tags and setting custom fields. " +
        "To move a card between columns/lanes use 'move_card'; to change assignees use 'assign_card'. Only the " +
        "provided fields are changed.")]
    public async Task<CardActionResult> UpdateCardAsync(
        [Description("The numeric id of the card to update.")]
        int cardId,
        [Description("New title for the card.")]
        string? title = null,
        [Description("New description/body for the card.")]
        string? description = null,
        [Description("New numeric priority.")]
        int? priority = null,
        [Description("New size/estimate.")]
        decimal? size = null,
        [Description("New card type id.")]
        int? typeId = null,
        [Description("New card color as a hex string (for example 'ff0000').")]
        string? color = null,
        [Description("New deadline in ISO 8601 format. Pass an empty string to clear the deadline.")]
        string? deadline = null,
        [Description("Set the blocked flag: true to block, false to unblock.")]
        bool? isBlocked = null,
        [Description("Tag ids to add to the card.")]
        int[]? addTagIds = null,
        [Description("Tag ids to remove from the card.")]
        int[]? removeTagIds = null,
        [Description("Custom field values to set, as pairs of field id and text value.")]
        CustomFieldValueInfo[]? customFields = null,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new ToolException("'cardId' must be a positive card id.");
        }

        var request = new UpdateCardRequest
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            Description = description,
            Priority = priority,
            Size = size,
            TypeId = typeId,
            Color = color,
            Deadline = deadline is null ? null : NormalizeDeadline(deadline) ?? string.Empty,
            IsBlocked = isBlocked is null ? null : (isBlocked.Value ? 1 : 0),
            TagIdsToAdd = addTagIds is { Length: > 0 } ? addTagIds : null,
            TagIdsToRemove = removeTagIds is { Length: > 0 } ? removeTagIds : null,
            CustomFieldsToAddOrUpdate = MapCustomFields(customFields),
        };

        if (!HasAnyUpdate(request))
        {
            throw new ToolException("No fields were provided to update. Specify at least one field to change.");
        }

        _logger.LogInformation("update_card card={CardId}", cardId);

        var updated = await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardAsync(cardId, request, cancellationToken),
            $"update card {cardId}").ConfigureAwait(false);

        return new CardActionResult(
            updated.CardId,
            updated.Title,
            updated.BoardId,
            updated.ColumnId,
            updated.LaneId,
            $"Updated card {updated.CardId}.");
    }

    /// <summary>
    /// Moves a card to another column, lane and/or position, validating that the destination exists
    /// on the card's board before moving.
    /// </summary>
    /// <remarks>
    /// Example: move a card to the "In Progress" column:
    /// <code>
    /// move_card(cardId: 12345, columnName: "In Progress")
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "move_card")]
    [Description(
        "Move a card to a different column, lane and/or position. The destination column/lane may be given by id " +
        "or by name and is validated against the card's board before moving, so invalid destinations fail with a " +
        "clear message instead of silently. At least one of column, lane or position must be provided.")]
    public async Task<CardActionResult> MoveCardAsync(
        [Description("The numeric id of the card to move.")]
        int cardId,
        [Description("Destination column id. Provide this or 'columnName'.")]
        int? columnId = null,
        [Description("Destination column name (case-insensitive). Provide this or 'columnId'.")]
        string? columnName = null,
        [Description("Destination lane id. Provide this or 'laneName'.")]
        int? laneId = null,
        [Description("Destination lane name (case-insensitive). Provide this or 'laneId'.")]
        string? laneName = null,
        [Description("Destination 0-based position within the target column/lane.")]
        int? position = null,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new ToolException("'cardId' must be a positive card id.");
        }

        var wantsColumn = columnId is not null || !string.IsNullOrWhiteSpace(columnName);
        var wantsLane = laneId is not null || !string.IsNullOrWhiteSpace(laneName);

        if (!wantsColumn && !wantsLane && position is null)
        {
            throw new ToolException(
                "Provide at least one destination: 'columnId'/'columnName', 'laneId'/'laneName' or 'position'.");
        }

        var card = await _context.ExecuteAsync(
            () => _context.Client.Cards.GetCardAsync(cardId, cancellationToken),
            $"read card {cardId}").ConfigureAwait(false);

        var board = await _context.ResolveBoardAsync(card.BoardId, null, cancellationToken).ConfigureAwait(false);
        var structure = await _context.GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);

        var targetColumnId = wantsColumn ? ResolveColumn(structure, columnId, columnName, required: true) : (int?)null;
        var targetLaneId = wantsLane ? ResolveLane(structure, laneId, laneName, required: true) : null;

        var request = new UpdateCardRequest
        {
            ColumnId = targetColumnId,
            LaneId = targetLaneId,
            Position = position,
        };

        _logger.LogInformation(
            "move_card card={CardId} column={ColumnId} lane={LaneId} position={Position}",
            cardId,
            targetColumnId,
            targetLaneId,
            position);

        var moved = await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardAsync(cardId, request, cancellationToken),
            $"move card {cardId}").ConfigureAwait(false);

        var destination = BuildDestinationText(structure, moved.ColumnId, moved.LaneId, moved.Position);
        return new CardActionResult(
            moved.CardId,
            moved.Title,
            moved.BoardId,
            moved.ColumnId,
            moved.LaneId,
            $"Moved card {moved.CardId} to {destination}.");
    }

    /// <summary>
    /// Assigns or reassigns a card. Sets the primary owner (assignee) and/or adds/removes co-owners.
    /// </summary>
    /// <remarks>
    /// Example: assign to the current user and add a co-owner:
    /// <code>
    /// assign_card(cardId: 12345, assignToMe: true, addCoOwnerUserIds: [42])
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "assign_card")]
    [Description(
        "Assign or reassign a card. Set the primary owner (assignee) by user id or to the current user, and/or add " +
        "and remove co-owners (secondary assignees). At least one assignment change must be provided. Note: users " +
        "must be referenced by id; there is no server-side user directory to resolve arbitrary names.")]
    public async Task<CardActionResult> AssignCardAsync(
        [Description("The numeric id of the card to assign.")]
        int cardId,
        [Description("The user id to set as the primary owner (assignee).")]
        int? assigneeUserId = null,
        [Description("When true, set the current authenticated user as the primary owner.")]
        bool assignToMe = false,
        [Description("User ids to add as co-owners (secondary assignees).")]
        int[]? addCoOwnerUserIds = null,
        [Description("User ids to remove from the co-owners.")]
        int[]? removeCoOwnerUserIds = null,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new ToolException("'cardId' must be a positive card id.");
        }

        var ownerId = await ResolveOwnerAsync(assigneeUserId, assignToMe, cancellationToken).ConfigureAwait(false);
        var hasCoOwnerChanges = addCoOwnerUserIds is { Length: > 0 } || removeCoOwnerUserIds is { Length: > 0 };

        if (ownerId is null && !hasCoOwnerChanges)
        {
            throw new ToolException(
                "Provide an assignment change: 'assigneeUserId', 'assignToMe', 'addCoOwnerUserIds' or 'removeCoOwnerUserIds'.");
        }

        var request = new UpdateCardRequest
        {
            OwnerUserId = ownerId,
            CoOwnerIdsToAdd = addCoOwnerUserIds is { Length: > 0 } ? addCoOwnerUserIds : null,
            CoOwnerIdsToRemove = removeCoOwnerUserIds is { Length: > 0 } ? removeCoOwnerUserIds : null,
        };

        _logger.LogInformation("assign_card card={CardId} owner={Owner}", cardId, ownerId);

        var updated = await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardAsync(cardId, request, cancellationToken),
            $"assign card {cardId}").ConfigureAwait(false);

        var summary = ownerId is int owner
            ? $"Set owner of card {updated.CardId} to user {owner}."
            : $"Updated co-owners of card {updated.CardId}.";

        return new CardActionResult(
            updated.CardId,
            updated.Title,
            updated.BoardId,
            updated.ColumnId,
            updated.LaneId,
            summary);
    }

    private async Task<FindCardsResult> SearchServerPagedAsync(
        int? boardId,
        int? ownerId,
        int[]? workflowIds,
        int[]? columnIds,
        int[]? laneIds,
        int[]? tagIds,
        int[]? typeIds,
        int[]? priorities,
        CardState? state,
        bool? isBlocked,
        int page,
        int pageSize,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(
            boardId, ownerId, workflowIds, columnIds, laneIds, tagIds, typeIds, priorities, state, isBlocked);
        query.Page = page;
        query.PerPage = pageSize;

        var result = await _context.ExecuteAsync(
            () => _context.Client.Cards.GetCardsAsync(query, cancellationToken),
            "search cards").ConfigureAwait(false);

        var cards = result.Data.Select(BusinessMapToolContext.ToSummary).ToList();
        var pagination = result.Pagination;
        var hasMore = pagination is not null && pagination.CurrentPage < pagination.AllPages;

        return new FindCardsResult(
            cards,
            pagination?.CurrentPage ?? page,
            pagination?.ResultsPerPage ?? pageSize,
            cards.Count,
            TotalMatches: null,
            hasMore,
            notes.Count > 0 ? string.Join(" ", notes) : null);
    }

    private async Task<FindCardsResult> SearchWithLocalFiltersAsync(
        string? text,
        int? boardId,
        int? ownerId,
        int[]? workflowIds,
        int[]? columnIds,
        int[]? laneIds,
        int[]? tagIds,
        int[]? typeIds,
        int[]? priorities,
        CardState? state,
        bool? isBlocked,
        DateTimeOffset? deadlineFrom,
        DateTimeOffset? deadlineTo,
        int page,
        int pageSize,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(
            boardId, ownerId, workflowIds, columnIds, laneIds, tagIds, typeIds, priorities, state, isBlocked);

        var matches = new List<Card>();
        var scanned = 0;
        var currentPage = 1;
        const int fetchSize = 100;
        var truncated = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            query.Page = currentPage;
            query.PerPage = fetchSize;

            var result = await _context.ExecuteAsync(
                () => _context.Client.Cards.GetCardsAsync(query, cancellationToken),
                "search cards").ConfigureAwait(false);

            foreach (var card in result.Data)
            {
                scanned++;
                if (MatchesLocalFilters(card, text, deadlineFrom, deadlineTo))
                {
                    matches.Add(card);
                }
            }

            var pagination = result.Pagination;
            var moreServerPages = pagination is not null && pagination.CurrentPage < pagination.AllPages;

            if (!moreServerPages || result.Data.Count == 0)
            {
                break;
            }

            if (scanned >= MaxScanCards)
            {
                truncated = true;
                break;
            }

            currentPage++;
        }

        var totalMatches = matches.Count;
        var skip = (page - 1) * pageSize;
        var pageItems = matches
            .Skip(skip)
            .Take(pageSize)
            .Select(BusinessMapToolContext.ToSummary)
            .ToList();

        var hasMore = skip + pageItems.Count < totalMatches || truncated;

        if (truncated)
        {
            notes.Add(
                $"Stopped after scanning {scanned} cards; refine filters (e.g. add a board) for complete results.");
        }

        return new FindCardsResult(
            pageItems,
            page,
            pageSize,
            pageItems.Count,
            totalMatches,
            hasMore,
            notes.Count > 0 ? string.Join(" ", notes) : null);
    }

    private static CardsQuery BuildQuery(
        int? boardId,
        int? ownerId,
        int[]? workflowIds,
        int[]? columnIds,
        int[]? laneIds,
        int[]? tagIds,
        int[]? typeIds,
        int[]? priorities,
        CardState? state,
        bool? isBlocked) => new()
        {
            BoardIds = boardId is int b ? [b] : null,
            OwnerUserIds = ownerId is int o ? [o] : null,
            WorkflowIds = ToNullableList(workflowIds),
            ColumnIds = ToNullableList(columnIds),
            LaneIds = ToNullableList(laneIds),
            TagIds = ToNullableList(tagIds),
            TypeIds = ToNullableList(typeIds),
            Priorities = ToNullableList(priorities),
            State = state,
            IsBlocked = isBlocked,
        };

    private static bool MatchesLocalFilters(
        Card card,
        string? text,
        DateTimeOffset? deadlineFrom,
        DateTimeOffset? deadlineTo)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            var haystack = string.Join(
                '\n',
                card.Title ?? string.Empty,
                card.Description ?? string.Empty,
                card.CustomId ?? string.Empty);

            if (!haystack.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (deadlineFrom is not null || deadlineTo is not null)
        {
            var deadline = BusinessMapToolContext.ParseDate(card.Deadline);
            if (deadline is null)
            {
                return false;
            }

            if (deadlineFrom is not null && deadline < deadlineFrom)
            {
                return false;
            }

            if (deadlineTo is not null && deadline > deadlineTo)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<int?> ResolveOwnerAsync(int? assigneeUserId, bool assignToMe, CancellationToken cancellationToken)
    {
        if (assignToMe)
        {
            var me = await _context.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
            if (assigneeUserId is not null && assigneeUserId != me)
            {
                throw new ToolException(
                    "Specify either 'assigneeUserId' or 'assignToMe', not both with different users.");
            }

            return me;
        }

        return assigneeUserId;
    }

    private static int ResolveColumn(BoardStructure structure, int? columnId, string? columnName, bool required)
    {
        if (columnId is int id)
        {
            if (!structure.ColumnExists(id))
            {
                throw new ToolException(
                    $"Column id {id} does not exist on board '{structure.BoardName}' (id {structure.BoardId}). " +
                    "Use 'get_workflow' to see valid columns.");
            }

            return id;
        }

        if (!string.IsNullOrWhiteSpace(columnName))
        {
            var matches = structure.FindColumnsByName(columnName.Trim());
            return matches.Count switch
            {
                1 => matches[0].ColumnId,
                0 => throw new ToolException(
                    $"No column named '{columnName}' exists on board '{structure.BoardName}' (id {structure.BoardId}). " +
                    "Use 'get_workflow' to see valid columns."),
                _ => throw new ToolException(
                    $"The column name '{columnName}' is ambiguous on board '{structure.BoardName}' and matches ids: " +
                    string.Join(", ", matches.Select(m => m.ColumnId)) + ". Provide 'columnId' instead."),
            };
        }

        throw new ToolException("A target column is required. Provide 'columnId' or 'columnName'.");
    }

    private static int? ResolveLane(BoardStructure structure, int? laneId, string? laneName, bool required)
    {
        if (laneId is int id)
        {
            if (!structure.LaneExists(id))
            {
                throw new ToolException(
                    $"Lane id {id} does not exist on board '{structure.BoardName}' (id {structure.BoardId}). " +
                    "Use 'get_workflow' to see valid lanes.");
            }

            return id;
        }

        if (!string.IsNullOrWhiteSpace(laneName))
        {
            var matches = structure.FindLanesByName(laneName.Trim());
            return matches.Count switch
            {
                1 => matches[0].LaneId,
                0 => throw new ToolException(
                    $"No lane named '{laneName}' exists on board '{structure.BoardName}' (id {structure.BoardId}). " +
                    "Use 'get_workflow' to see valid lanes."),
                _ => throw new ToolException(
                    $"The lane name '{laneName}' is ambiguous on board '{structure.BoardName}' and matches ids: " +
                    string.Join(", ", matches.Select(m => m.LaneId)) + ". Provide 'laneId' instead."),
            };
        }

        if (required)
        {
            throw new ToolException("A target lane is required. Provide 'laneId' or 'laneName'.");
        }

        return null;
    }

    private static string BuildDestinationText(BoardStructure structure, int? columnId, int? laneId, int? position)
    {
        var parts = new List<string>();
        var columnName = structure.GetColumnName(columnId);
        parts.Add(columnName is null ? $"column {columnId}" : $"column '{columnName}'");

        var laneName = structure.GetLaneName(laneId);
        if (laneName is not null)
        {
            parts.Add($"lane '{laneName}'");
        }
        else if (laneId is not null)
        {
            parts.Add($"lane {laneId}");
        }

        if (position is not null)
        {
            parts.Add($"position {position}");
        }

        return string.Join(", ", parts);
    }

    private static IReadOnlyList<int>? ToNullableList(int[]? values) =>
        values is { Length: > 0 } ? values : null;

    private static IEnumerable<CardCustomFieldValue>? MapCustomFields(CustomFieldValueInfo[]? customFields) =>
        customFields is { Length: > 0 }
            ? customFields.Select(f => new CardCustomFieldValue { FieldId = f.FieldId, Value = f.Value }).ToList()
            : null;

    private static bool HasAnyUpdate(UpdateCardRequest request) =>
        request.Title is not null ||
        request.Description is not null ||
        request.Priority is not null ||
        request.Size is not null ||
        request.TypeId is not null ||
        request.Color is not null ||
        request.Deadline is not null ||
        request.IsBlocked is not null ||
        request.TagIdsToAdd is not null ||
        request.TagIdsToRemove is not null ||
        request.CustomFieldsToAddOrUpdate is not null;

    private static CardState? ParseState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        return state.Trim().ToLowerInvariant() switch
        {
            "active" => CardState.Active,
            "archived" => CardState.Archived,
            "discarded" => CardState.Discarded,
            _ => throw new ToolException(
                $"Invalid state '{state}'. Valid values are 'active', 'archived' or 'discarded'."),
        };
    }

    private static DateTimeOffset? RequireValidDate(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return BusinessMapToolContext.ParseDate(value)
            ?? throw new ToolException($"'{parameterName}' is not a valid ISO 8601 date/time: '{value}'.");
    }

    private static string? NormalizeDeadline(string? deadline)
    {
        if (string.IsNullOrWhiteSpace(deadline))
        {
            return null;
        }

        return BusinessMapToolContext.ParseDate(deadline) is not null
            ? deadline.Trim()
            : throw new ToolException($"'deadline' is not a valid ISO 8601 date/time: '{deadline}'.");
    }
}
