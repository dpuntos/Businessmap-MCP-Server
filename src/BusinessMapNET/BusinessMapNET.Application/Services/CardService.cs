using BusinessMapNET.Application.Internal;
using BusinessMapNET.Application.Models;
using BusinessMapNET.Core.Models;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Services;

/// <inheritdoc />
public sealed class CardService : ICardService
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;
    private const int MaxScanCards = 500;

    private readonly BusinessMapContext _context;
    private readonly ILogger<CardService> _logger;

    /// <summary>Initializes a new instance of the <see cref="CardService"/> class.</summary>
    public CardService(BusinessMapContext context, ILogger<CardService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CardSearchResult> FindCardsAsync(
        CardSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (criteria.Page < 1)
        {
            throw new BusinessMapServiceException("'page' must be greater than or equal to 1.");
        }

        var pageSize = Math.Clamp(criteria.PageSize, 1, MaxPageSize);
        var stateFilter = ParseState(criteria.State);
        var deadlineFrom = RequireValidDate(criteria.DeadlineAfter, nameof(criteria.DeadlineAfter));
        var deadlineTo = RequireValidDate(criteria.DeadlineBefore, nameof(criteria.DeadlineBefore));

        var notes = new List<string>();

        int? resolvedBoardId = criteria.BoardId;
        if (resolvedBoardId is null && !string.IsNullOrWhiteSpace(criteria.BoardName))
        {
            var board = await _context.ResolveBoardAsync(null, criteria.BoardName, cancellationToken)
                .ConfigureAwait(false);
            resolvedBoardId = board.BoardId;
            notes.Add($"Resolved board '{criteria.BoardName}' to id {board.BoardId}.");
        }

        int? ownerFilter = criteria.AssigneeUserId;
        if (criteria.AssignedToMe)
        {
            var me = await _context.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
            if (ownerFilter is not null && ownerFilter != me)
            {
                throw new BusinessMapServiceException(
                    "Specify either 'assigneeUserId' or 'assignedToMe', not both with different users.");
            }

            ownerFilter = me;
        }

        var hasLocalFilters =
            !string.IsNullOrWhiteSpace(criteria.Text) || deadlineFrom is not null || deadlineTo is not null;

        _logger.LogInformation(
            "find_cards board={BoardId} owner={Owner} localFilters={HasLocal} page={Page} pageSize={PageSize}",
            resolvedBoardId,
            ownerFilter,
            hasLocalFilters,
            criteria.Page,
            pageSize);

        return hasLocalFilters
            ? await SearchWithLocalFiltersAsync(
                criteria, resolvedBoardId, ownerFilter, stateFilter, deadlineFrom, deadlineTo,
                pageSize, notes, cancellationToken).ConfigureAwait(false)
            : await SearchServerPagedAsync(
                criteria, resolvedBoardId, ownerFilter, stateFilter, pageSize, notes, cancellationToken)
                .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CardDetail> GetCardDetailsAsync(
        int cardId,
        bool includeComments,
        bool includeSubtasks,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new BusinessMapServiceException("'cardId' must be a positive card id.");
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
        catch (BusinessMapServiceException ex)
        {
            _logger.LogWarning(ex, "Could not resolve board name for card {CardId}.", cardId);
        }

        return new CardDetail(card, boardName, comments, subtasks);
    }

    /// <inheritdoc />
    public async Task<CardActionOutcome> CreateCardAsync(
        CreateCardInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Title))
        {
            throw new BusinessMapServiceException("'title' is required and cannot be empty.");
        }

        var deadlineValue = NormalizeDeadline(input.Deadline);
        var board = await _context.ResolveBoardAsync(input.BoardId, input.BoardName, cancellationToken)
            .ConfigureAwait(false);
        var structure = await _context.GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);

        var resolvedColumnId = ResolveColumn(structure, input.ColumnId, input.ColumnName, required: true);
        var resolvedLaneId = ResolveLane(structure, input.LaneId, input.LaneName, required: false);

        if (input.TypeId is int type && !structure.CardTypeExists(type))
        {
            throw new BusinessMapServiceException(
                $"Card type id {type} does not exist on board '{board.Name}' (id {board.BoardId}). " +
                "Use 'get_workflow' to see the valid card types.");
        }

        var ownerId = await ResolveOwnerAsync(input.AssigneeUserId, input.AssignToMe, cancellationToken)
            .ConfigureAwait(false);

        var request = new CreateCardRequest
        {
            Title = input.Title.Trim(),
            ColumnId = resolvedColumnId,
            LaneId = resolvedLaneId,
            Description = input.Description,
            OwnerUserId = ownerId,
            Priority = input.Priority,
            Size = input.Size,
            TypeId = input.TypeId,
            Color = input.Color,
            Deadline = deadlineValue,
            TagIdsToAdd = input.TagIds is { Length: > 0 } ? input.TagIds : null,
            CustomFieldsToAddOrUpdate = MapCustomFields(input.CustomFields),
        };

        _logger.LogInformation(
            "create_card board={BoardId} column={ColumnId} lane={LaneId}",
            board.BoardId,
            resolvedColumnId,
            resolvedLaneId);

        var created = await _context.ExecuteAsync(
            () => _context.Client.Cards.CreateCardAsync(request, cancellationToken),
            $"create a card on board {board.BoardId}").ConfigureAwait(false);

        return new CardActionOutcome(
            created.CardId,
            created.Title,
            created.BoardId,
            created.ColumnId,
            created.LaneId,
            $"Created card {created.CardId} on board '{board.Name}' (id {board.BoardId}).");
    }

    /// <inheritdoc />
    public async Task<CardActionOutcome> UpdateCardAsync(
        UpdateCardInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.CardId <= 0)
        {
            throw new BusinessMapServiceException("'cardId' must be a positive card id.");
        }

        var request = new UpdateCardRequest
        {
            Title = string.IsNullOrWhiteSpace(input.Title) ? null : input.Title.Trim(),
            Description = input.Description,
            Priority = input.Priority,
            Size = input.Size,
            TypeId = input.TypeId,
            Color = input.Color,
            Deadline = input.Deadline is null ? null : NormalizeDeadline(input.Deadline) ?? string.Empty,
            IsBlocked = input.IsBlocked is null ? null : (input.IsBlocked.Value ? 1 : 0),
            TagIdsToAdd = input.AddTagIds is { Length: > 0 } ? input.AddTagIds : null,
            TagIdsToRemove = input.RemoveTagIds is { Length: > 0 } ? input.RemoveTagIds : null,
            CustomFieldsToAddOrUpdate = MapCustomFields(input.CustomFields),
        };

        if (!HasAnyUpdate(request))
        {
            throw new BusinessMapServiceException(
                "No fields were provided to update. Specify at least one field to change.");
        }

        _logger.LogInformation("update_card card={CardId}", input.CardId);

        var updated = await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardAsync(input.CardId, request, cancellationToken),
            $"update card {input.CardId}").ConfigureAwait(false);

        return new CardActionOutcome(
            updated.CardId,
            updated.Title,
            updated.BoardId,
            updated.ColumnId,
            updated.LaneId,
            $"Updated card {updated.CardId}.");
    }

    /// <inheritdoc />
    public async Task<CardActionOutcome> MoveCardAsync(
        MoveCardInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.CardId <= 0)
        {
            throw new BusinessMapServiceException("'cardId' must be a positive card id.");
        }

        var wantsColumn = input.ColumnId is not null || !string.IsNullOrWhiteSpace(input.ColumnName);
        var wantsLane = input.LaneId is not null || !string.IsNullOrWhiteSpace(input.LaneName);

        if (!wantsColumn && !wantsLane && input.Position is null)
        {
            throw new BusinessMapServiceException(
                "Provide at least one destination: 'columnId'/'columnName', 'laneId'/'laneName' or 'position'.");
        }

        var card = await _context.ExecuteAsync(
            () => _context.Client.Cards.GetCardAsync(input.CardId, cancellationToken),
            $"read card {input.CardId}").ConfigureAwait(false);

        var board = await _context.ResolveBoardAsync(card.BoardId, null, cancellationToken).ConfigureAwait(false);
        var structure = await _context.GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);

        var targetColumnId = wantsColumn
            ? ResolveColumn(structure, input.ColumnId, input.ColumnName, required: true)
            : (int?)null;
        var targetLaneId = wantsLane
            ? ResolveLane(structure, input.LaneId, input.LaneName, required: true)
            : null;

        var request = new UpdateCardRequest
        {
            ColumnId = targetColumnId,
            LaneId = targetLaneId,
            Position = input.Position,
        };

        _logger.LogInformation(
            "move_card card={CardId} column={ColumnId} lane={LaneId} position={Position}",
            input.CardId,
            targetColumnId,
            targetLaneId,
            input.Position);

        var moved = await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardAsync(input.CardId, request, cancellationToken),
            $"move card {input.CardId}").ConfigureAwait(false);

        var destination = BuildDestinationText(structure, moved.ColumnId, moved.LaneId, moved.Position);
        return new CardActionOutcome(
            moved.CardId,
            moved.Title,
            moved.BoardId,
            moved.ColumnId,
            moved.LaneId,
            $"Moved card {moved.CardId} to {destination}.");
    }

    /// <inheritdoc />
    public async Task<CardActionOutcome> AssignCardAsync(
        AssignCardInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.CardId <= 0)
        {
            throw new BusinessMapServiceException("'cardId' must be a positive card id.");
        }

        var ownerId = await ResolveOwnerAsync(input.AssigneeUserId, input.AssignToMe, cancellationToken)
            .ConfigureAwait(false);
        var hasCoOwnerChanges =
            input.AddCoOwnerUserIds is { Length: > 0 } || input.RemoveCoOwnerUserIds is { Length: > 0 };

        if (ownerId is null && !hasCoOwnerChanges)
        {
            throw new BusinessMapServiceException(
                "Provide an assignment change: 'assigneeUserId', 'assignToMe', 'addCoOwnerUserIds' or 'removeCoOwnerUserIds'.");
        }

        var request = new UpdateCardRequest
        {
            OwnerUserId = ownerId,
            CoOwnerIdsToAdd = input.AddCoOwnerUserIds is { Length: > 0 } ? input.AddCoOwnerUserIds : null,
            CoOwnerIdsToRemove = input.RemoveCoOwnerUserIds is { Length: > 0 } ? input.RemoveCoOwnerUserIds : null,
        };

        _logger.LogInformation("assign_card card={CardId} owner={Owner}", input.CardId, ownerId);

        var updated = await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardAsync(input.CardId, request, cancellationToken),
            $"assign card {input.CardId}").ConfigureAwait(false);

        var summary = ownerId is int owner
            ? $"Set owner of card {updated.CardId} to user {owner}."
            : $"Updated co-owners of card {updated.CardId}.";

        return new CardActionOutcome(
            updated.CardId,
            updated.Title,
            updated.BoardId,
            updated.ColumnId,
            updated.LaneId,
            summary);
    }

    private async Task<CardSearchResult> SearchServerPagedAsync(
        CardSearchCriteria criteria,
        int? boardId,
        int? ownerId,
        CardState? state,
        int pageSize,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(criteria, boardId, ownerId, state);
        query.Page = criteria.Page;
        query.PerPage = pageSize;

        var result = await _context.ExecuteAsync(
            () => _context.Client.Cards.GetCardsAsync(query, cancellationToken),
            "search cards").ConfigureAwait(false);

        var cards = result.Data.ToList();
        var pagination = result.Pagination;
        var hasMore = pagination is not null && pagination.CurrentPage < pagination.AllPages;

        return new CardSearchResult(
            cards,
            pagination?.CurrentPage ?? criteria.Page,
            pagination?.ResultsPerPage ?? pageSize,
            cards.Count,
            TotalMatches: null,
            hasMore,
            notes.Count > 0 ? string.Join(" ", notes) : null);
    }

    private async Task<CardSearchResult> SearchWithLocalFiltersAsync(
        CardSearchCriteria criteria,
        int? boardId,
        int? ownerId,
        CardState? state,
        DateTimeOffset? deadlineFrom,
        DateTimeOffset? deadlineTo,
        int pageSize,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(criteria, boardId, ownerId, state);

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
                if (MatchesLocalFilters(card, criteria.Text, deadlineFrom, deadlineTo))
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
        var skip = (criteria.Page - 1) * pageSize;
        var pageItems = matches
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        var hasMore = skip + pageItems.Count < totalMatches || truncated;

        if (truncated)
        {
            notes.Add(
                $"Stopped after scanning {scanned} cards; refine filters (e.g. add a board) for complete results.");
        }

        return new CardSearchResult(
            pageItems,
            criteria.Page,
            pageSize,
            pageItems.Count,
            totalMatches,
            hasMore,
            notes.Count > 0 ? string.Join(" ", notes) : null);
    }

    private static CardsQuery BuildQuery(
        CardSearchCriteria criteria,
        int? boardId,
        int? ownerId,
        CardState? state) => new()
        {
            BoardIds = boardId is int b ? [b] : null,
            OwnerUserIds = ownerId is int o ? [o] : null,
            WorkflowIds = ToNullableList(criteria.WorkflowIds),
            ColumnIds = ToNullableList(criteria.ColumnIds),
            LaneIds = ToNullableList(criteria.LaneIds),
            TagIds = ToNullableList(criteria.TagIds),
            TypeIds = ToNullableList(criteria.TypeIds),
            Priorities = ToNullableList(criteria.Priorities),
            State = state,
            IsBlocked = criteria.IsBlocked,
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
            var deadline = DateParsing.Parse(card.Deadline);
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

    private async Task<int?> ResolveOwnerAsync(
        int? assigneeUserId,
        bool assignToMe,
        CancellationToken cancellationToken)
    {
        if (assignToMe)
        {
            var me = await _context.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
            if (assigneeUserId is not null && assigneeUserId != me)
            {
                throw new BusinessMapServiceException(
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
                throw new BusinessMapServiceException(
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
                0 => throw new BusinessMapServiceException(
                    $"No column named '{columnName}' exists on board '{structure.BoardName}' (id {structure.BoardId}). " +
                    "Use 'get_workflow' to see valid columns."),
                _ => throw new BusinessMapServiceException(
                    $"The column name '{columnName}' is ambiguous on board '{structure.BoardName}' and matches ids: " +
                    string.Join(", ", matches.Select(m => m.ColumnId)) + ". Provide 'columnId' instead."),
            };
        }

        throw new BusinessMapServiceException("A target column is required. Provide 'columnId' or 'columnName'.");
    }

    private static int? ResolveLane(BoardStructure structure, int? laneId, string? laneName, bool required)
    {
        if (laneId is int id)
        {
            if (!structure.LaneExists(id))
            {
                throw new BusinessMapServiceException(
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
                0 => throw new BusinessMapServiceException(
                    $"No lane named '{laneName}' exists on board '{structure.BoardName}' (id {structure.BoardId}). " +
                    "Use 'get_workflow' to see valid lanes."),
                _ => throw new BusinessMapServiceException(
                    $"The lane name '{laneName}' is ambiguous on board '{structure.BoardName}' and matches ids: " +
                    string.Join(", ", matches.Select(m => m.LaneId)) + ". Provide 'laneId' instead."),
            };
        }

        if (required)
        {
            throw new BusinessMapServiceException("A target lane is required. Provide 'laneId' or 'laneName'.");
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

    private static IEnumerable<CardCustomFieldValue>? MapCustomFields(CustomFieldValue[]? customFields) =>
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
            _ => throw new BusinessMapServiceException(
                $"Invalid state '{state}'. Valid values are 'active', 'archived' or 'discarded'."),
        };
    }

    private static DateTimeOffset? RequireValidDate(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateParsing.Parse(value)
            ?? throw new BusinessMapServiceException(
                $"'{parameterName}' is not a valid ISO 8601 date/time: '{value}'.");
    }

    private static string? NormalizeDeadline(string? deadline)
    {
        if (string.IsNullOrWhiteSpace(deadline))
        {
            return null;
        }

        return DateParsing.Parse(deadline) is not null
            ? deadline.Trim()
            : throw new BusinessMapServiceException($"'deadline' is not a valid ISO 8601 date/time: '{deadline}'.");
    }
}
