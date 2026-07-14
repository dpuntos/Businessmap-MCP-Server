using System.ComponentModel;
using BusinessMapNET.Application.Models;
using BusinessMapNET.Application.Services;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to search, inspect and manipulate cards. These tools are thin adapters over
/// <see cref="ICardService"/>: they translate MCP parameters into service calls and map the results
/// back into serializable DTOs.
/// </summary>
[McpServerToolType]
public sealed class CardTools
{
    private readonly ICardService _cardService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardTools"/> class.
    /// </summary>
    public CardTools(ICardService cardService)
    {
        _cardService = cardService ?? throw new ArgumentNullException(nameof(cardService));
    }

    /// <summary>
    /// Searches for cards using any combination of server-side and local filters.
    /// </summary>
    /// <remarks>
    /// Example: <c>find_cards(boardName: "Delivery", assignedToMe: true, state: "active")</c>.
    /// </remarks>
    [McpServerTool(Name = "find_cards")]
    [Description(
        "Search for cards using any combination of filters: free text, board (id or name), assignee (by id or the " +
        "current user), workflow/column/lane ids, tags, card types, priorities, lifecycle state ('active', " +
        "'archived', 'discarded'), blocked flag and deadline range. Returns a paginated list of card summaries. " +
        "Use 'get_workflow' to discover column/lane/type ids and 'list_users' to resolve an 'assigneeUserId'.")]
    public Task<FindCardsResult> FindCardsAsync(
        [Description("Free text to match against the card title, description or custom id.")]
        string? text = null,
        [Description("Restrict to a board by its numeric id. Provide this or 'boardName'.")]
        int? boardId = null,
        [Description("Restrict to a board by its name (case-insensitive). Provide this or 'boardId'.")]
        string? boardName = null,
        [Description("Restrict to cards owned by this user id.")]
        int? assigneeUserId = null,
        [Description("When true, restrict to cards owned by the current user.")]
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
        [Description("Lifecycle state: 'active', 'archived' or 'discarded'.")]
        string? state = null,
        [Description("Restrict to blocked (true) or unblocked (false) cards.")]
        bool? isBlocked = null,
        [Description("Only include cards with a deadline on or after this ISO 8601 date/time.")]
        string? deadlineAfter = null,
        [Description("Only include cards with a deadline on or before this ISO 8601 date/time.")]
        string? deadlineBefore = null,
        [Description("1-based page number to return. Defaults to 1.")]
        int page = 1,
        [Description("Maximum cards per page. Defaults to 25.")]
        int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var criteria = new CardSearchCriteria
            {
                Text = text,
                BoardId = boardId,
                BoardName = boardName,
                AssigneeUserId = assigneeUserId,
                AssignedToMe = assignedToMe,
                WorkflowIds = workflowIds,
                ColumnIds = columnIds,
                LaneIds = laneIds,
                TagIds = tagIds,
                TypeIds = typeIds,
                Priorities = priorities,
                State = state,
                IsBlocked = isBlocked,
                DeadlineAfter = deadlineAfter,
                DeadlineBefore = deadlineBefore,
                Page = page,
                PageSize = pageSize
            };
            var result = await _cardService.FindCardsAsync(criteria, cancellationToken).ConfigureAwait(false);
            return DtoMapper.ToDto(result);
        });

    /// <summary>
    /// Returns the full detail of a card, optionally with comments and subtasks.
    /// </summary>
    /// <remarks>
    /// Example: <c>get_card_details(cardId: 12345, includeComments: true, includeSubtasks: true)</c>.
    /// </remarks>
    [McpServerTool(Name = "get_card_details")]
    [Description(
        "Get the full detail of a card by id: title, description, board/column/lane, owner and co-owners, type, " +
        "size, priority, color, deadline, tags and custom fields. Optionally include comments and subtasks " +
        "(checklist items) with their ids, needed by 'complete_task' and other tools.")]
    public Task<CardDetails> GetCardDetailsAsync(
        [Description("The id of the card to inspect.")]
        int cardId,
        [Description("When true, include the card's comments. Defaults to true.")]
        bool includeComments = true,
        [Description("When true, include the card's subtasks (checklist items). Defaults to true.")]
        bool includeSubtasks = true,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var detail = await _cardService
                .GetCardDetailsAsync(cardId, includeComments, includeSubtasks, cancellationToken)
                .ConfigureAwait(false);
            return DtoMapper.ToDto(detail);
        });

    /// <summary>
    /// Creates a card on a board, resolving and validating the destination.
    /// </summary>
    /// <remarks>
    /// Example: <c>create_card(title: "Fix login bug", boardName: "Delivery", columnName: "To Do")</c>.
    /// </remarks>
    [McpServerTool(Name = "create_card")]
    [Description(
        "Create a card on a board. The destination board, column and lane can be given by id or by name. " +
        "Optionally set description, owner (by id or the current user), tags, priority, size, card type, color, " +
        "deadline and custom fields. Use 'get_workflow' to discover valid column/lane/type ids for the board.")]
    public Task<CardActionResult> CreateCardAsync(
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
        [Description("Target lane id. Provide this or 'laneName'.")]
        int? laneId = null,
        [Description("Target lane name (case-insensitive). Provide this or 'laneId'.")]
        string? laneName = null,
        [Description("The description/body of the card.")]
        string? description = null,
        [Description("The owner (assignee) user id.")]
        int? assigneeUserId = null,
        [Description("When true, assign the new card to the current user.")]
        bool assignToMe = false,
        [Description("Tag ids to attach to the card.")]
        int[]? tagIds = null,
        [Description("Numeric priority to set on the card.")]
        int? priority = null,
        [Description("Size/estimate to set on the card.")]
        decimal? size = null,
        [Description("Card type id, validated against the board when provided.")]
        int? typeId = null,
        [Description("Card color as a hex string.")]
        string? color = null,
        [Description("Deadline in ISO 8601 format.")]
        string? deadline = null,
        [Description("Custom field values to set, each as a field id and value.")]
        CustomFieldValue[]? customFields = null,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var input = new CreateCardInput
            {
                Title = title,
                BoardId = boardId,
                BoardName = boardName,
                ColumnId = columnId,
                ColumnName = columnName,
                LaneId = laneId,
                LaneName = laneName,
                Description = description,
                AssigneeUserId = assigneeUserId,
                AssignToMe = assignToMe,
                TagIds = tagIds,
                Priority = priority,
                Size = size,
                TypeId = typeId,
                Color = color,
                Deadline = deadline,
                CustomFields = customFields
            };
            var outcome = await _cardService.CreateCardAsync(input, cancellationToken).ConfigureAwait(false);
            return DtoMapper.ToDto(outcome);
        });

    /// <summary>
    /// Updates selected fields of a card without sending the whole object.
    /// </summary>
    /// <remarks>
    /// Example: <c>update_card(cardId: 12345, priority: 3, isBlocked: true)</c>.
    /// </remarks>
    [McpServerTool(Name = "update_card")]
    [Description(
        "Update selected fields of an existing card: title, description, priority, size, card type, color, " +
        "deadline (pass an empty string to clear it), blocked flag, tags to add/remove and custom fields. " +
        "Only the provided fields are changed. To move a card use 'move_card'; to reassign it use 'assign_card'.")]
    public Task<CardActionResult> UpdateCardAsync(
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
        [Description("New card color as a hex string.")]
        string? color = null,
        [Description("New deadline in ISO 8601 format. Pass an empty string to clear the deadline.")]
        string? deadline = null,
        [Description("Set the blocked flag: true to block, false to unblock.")]
        bool? isBlocked = null,
        [Description("Tag ids to add to the card.")]
        int[]? addTagIds = null,
        [Description("Tag ids to remove from the card.")]
        int[]? removeTagIds = null,
        [Description("Custom field values to set, each as a field id and value.")]
        CustomFieldValue[]? customFields = null,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var input = new UpdateCardInput
            {
                CardId = cardId,
                Title = title,
                Description = description,
                Priority = priority,
                Size = size,
                TypeId = typeId,
                Color = color,
                Deadline = deadline,
                IsBlocked = isBlocked,
                AddTagIds = addTagIds,
                RemoveTagIds = removeTagIds,
                CustomFields = customFields
            };
            var outcome = await _cardService.UpdateCardAsync(input, cancellationToken).ConfigureAwait(false);
            return DtoMapper.ToDto(outcome);
        });

    /// <summary>
    /// Moves a card to another column, lane and/or position.
    /// </summary>
    /// <remarks>
    /// Example: <c>move_card(cardId: 12345, columnName: "In Progress")</c>.
    /// </remarks>
    [McpServerTool(Name = "move_card")]
    [Description(
        "Move a card to another column, lane and/or position. The destination column and lane can be given by id " +
        "or by name. Use 'get_workflow' to discover valid column/lane ids for the card's board.")]
    public Task<CardActionResult> MoveCardAsync(
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
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var input = new MoveCardInput
            {
                CardId = cardId,
                ColumnId = columnId,
                ColumnName = columnName,
                LaneId = laneId,
                LaneName = laneName,
                Position = position
            };
            var outcome = await _cardService.MoveCardAsync(input, cancellationToken).ConfigureAwait(false);
            return DtoMapper.ToDto(outcome);
        });

    /// <summary>
    /// Assigns or reassigns a card (primary owner and/or co-owners).
    /// </summary>
    /// <remarks>
    /// Example: <c>assign_card(cardId: 12345, assigneeUserId: 42)</c>.
    /// </remarks>
    [McpServerTool(Name = "assign_card")]
    [Description(
        "Assign or reassign a card. Set the primary owner (by user id or to the current user) and/or add and " +
        "remove co-owners. Use 'list_users' to resolve a person's name into a user id.")]
    public Task<CardActionResult> AssignCardAsync(
        [Description("The numeric id of the card to assign.")]
        int cardId,
        [Description("The user id to set as the primary owner.")]
        int? assigneeUserId = null,
        [Description("When true, set the current user as the primary owner.")]
        bool assignToMe = false,
        [Description("User ids to add as co-owners.")]
        int[]? addCoOwnerUserIds = null,
        [Description("User ids to remove from the co-owners.")]
        int[]? removeCoOwnerUserIds = null,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var input = new AssignCardInput
            {
                CardId = cardId,
                AssigneeUserId = assigneeUserId,
                AssignToMe = assignToMe,
                AddCoOwnerUserIds = addCoOwnerUserIds,
                RemoveCoOwnerUserIds = removeCoOwnerUserIds
            };
            var outcome = await _cardService.AssignCardAsync(input, cancellationToken).ConfigureAwait(false);
            return DtoMapper.ToDto(outcome);
        });
}
