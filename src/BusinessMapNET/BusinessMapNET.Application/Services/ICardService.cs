using BusinessMapNET.Application.Models;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Application.Services;

/// <summary>Business operations to search, inspect and manipulate cards.</summary>
public interface ICardService
{
    /// <summary>Searches for cards using any combination of server-side and local filters.</summary>
    Task<CardSearchResult> FindCardsAsync(CardSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>Returns the full detail of a card, optionally with comments and subtasks.</summary>
    Task<CardDetail> GetCardDetailsAsync(
        int cardId,
        bool includeComments,
        bool includeSubtasks,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a card on a board, resolving and validating the destination.</summary>
    Task<CardActionOutcome> CreateCardAsync(CreateCardInput input, CancellationToken cancellationToken = default);

    /// <summary>Updates selected fields of a card without sending the whole object.</summary>
    Task<CardActionOutcome> UpdateCardAsync(UpdateCardInput input, CancellationToken cancellationToken = default);

    /// <summary>Moves a card to another column, lane and/or position.</summary>
    Task<CardActionOutcome> MoveCardAsync(MoveCardInput input, CancellationToken cancellationToken = default);

    /// <summary>Assigns or reassigns a card (primary owner and/or co-owners).</summary>
    Task<CardActionOutcome> AssignCardAsync(AssignCardInput input, CancellationToken cancellationToken = default);
}

/// <summary>Combinable filters for a card search.</summary>
public sealed record CardSearchCriteria
{
    /// <summary>Free text to match against the card title, description or custom id.</summary>
    public string? Text { get; init; }

    /// <summary>Restrict to a board by its numeric id.</summary>
    public int? BoardId { get; init; }

    /// <summary>Restrict to a board by its name (ignored when <see cref="BoardId"/> is set).</summary>
    public string? BoardName { get; init; }

    /// <summary>Restrict to cards owned by this user id.</summary>
    public int? AssigneeUserId { get; init; }

    /// <summary>When true, restrict to cards owned by the current user.</summary>
    public bool AssignedToMe { get; init; }

    /// <summary>Restrict to cards in these workflow ids.</summary>
    public int[]? WorkflowIds { get; init; }

    /// <summary>Restrict to cards in these column ids.</summary>
    public int[]? ColumnIds { get; init; }

    /// <summary>Restrict to cards in these lane ids.</summary>
    public int[]? LaneIds { get; init; }

    /// <summary>Restrict to cards tagged with these tag ids.</summary>
    public int[]? TagIds { get; init; }

    /// <summary>Restrict to cards of these card type ids.</summary>
    public int[]? TypeIds { get; init; }

    /// <summary>Restrict to cards with these numeric priorities.</summary>
    public int[]? Priorities { get; init; }

    /// <summary>Lifecycle state: 'active', 'archived' or 'discarded'.</summary>
    public string? State { get; init; }

    /// <summary>Restrict to blocked (true) or unblocked (false) cards.</summary>
    public bool? IsBlocked { get; init; }

    /// <summary>Only include cards with a deadline on or after this ISO 8601 date/time.</summary>
    public string? DeadlineAfter { get; init; }

    /// <summary>Only include cards with a deadline on or before this ISO 8601 date/time.</summary>
    public string? DeadlineBefore { get; init; }

    /// <summary>1-based page number to return.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Maximum cards per page.</summary>
    public int PageSize { get; init; } = 25;
}

/// <summary>Input to create a card.</summary>
public sealed record CreateCardInput
{
    /// <summary>The title of the new card. Required and non-empty.</summary>
    public required string Title { get; init; }

    /// <summary>Target board id (or use <see cref="BoardName"/>).</summary>
    public int? BoardId { get; init; }

    /// <summary>Target board name (or use <see cref="BoardId"/>).</summary>
    public string? BoardName { get; init; }

    /// <summary>Target column id (or use <see cref="ColumnName"/>).</summary>
    public int? ColumnId { get; init; }

    /// <summary>Target column name (or use <see cref="ColumnId"/>).</summary>
    public string? ColumnName { get; init; }

    /// <summary>Target lane id (or use <see cref="LaneName"/>).</summary>
    public int? LaneId { get; init; }

    /// <summary>Target lane name (or use <see cref="LaneId"/>).</summary>
    public string? LaneName { get; init; }

    /// <summary>The description/body of the card.</summary>
    public string? Description { get; init; }

    /// <summary>The owner (assignee) user id.</summary>
    public int? AssigneeUserId { get; init; }

    /// <summary>When true, assign the new card to the current user.</summary>
    public bool AssignToMe { get; init; }

    /// <summary>Tag ids to attach to the card.</summary>
    public int[]? TagIds { get; init; }

    /// <summary>Numeric priority to set on the card.</summary>
    public int? Priority { get; init; }

    /// <summary>Size/estimate to set on the card.</summary>
    public decimal? Size { get; init; }

    /// <summary>Card type id, validated against the board when provided.</summary>
    public int? TypeId { get; init; }

    /// <summary>Card color as a hex string.</summary>
    public string? Color { get; init; }

    /// <summary>Deadline in ISO 8601 format.</summary>
    public string? Deadline { get; init; }

    /// <summary>Custom field values to set.</summary>
    public CustomFieldValue[]? CustomFields { get; init; }
}

/// <summary>Input to update a card.</summary>
public sealed record UpdateCardInput
{
    /// <summary>The numeric id of the card to update.</summary>
    public required int CardId { get; init; }

    /// <summary>New title for the card.</summary>
    public string? Title { get; init; }

    /// <summary>New description/body for the card.</summary>
    public string? Description { get; init; }

    /// <summary>New numeric priority.</summary>
    public int? Priority { get; init; }

    /// <summary>New size/estimate.</summary>
    public decimal? Size { get; init; }

    /// <summary>New card type id.</summary>
    public int? TypeId { get; init; }

    /// <summary>New card color as a hex string.</summary>
    public string? Color { get; init; }

    /// <summary>New deadline in ISO 8601 format. Pass an empty string to clear the deadline.</summary>
    public string? Deadline { get; init; }

    /// <summary>Set the blocked flag: true to block, false to unblock.</summary>
    public bool? IsBlocked { get; init; }

    /// <summary>Tag ids to add to the card.</summary>
    public int[]? AddTagIds { get; init; }

    /// <summary>Tag ids to remove from the card.</summary>
    public int[]? RemoveTagIds { get; init; }

    /// <summary>Custom field values to set.</summary>
    public CustomFieldValue[]? CustomFields { get; init; }
}

/// <summary>Input to move a card.</summary>
public sealed record MoveCardInput
{
    /// <summary>The numeric id of the card to move.</summary>
    public required int CardId { get; init; }

    /// <summary>Destination column id (or use <see cref="ColumnName"/>).</summary>
    public int? ColumnId { get; init; }

    /// <summary>Destination column name (or use <see cref="ColumnId"/>).</summary>
    public string? ColumnName { get; init; }

    /// <summary>Destination lane id (or use <see cref="LaneName"/>).</summary>
    public int? LaneId { get; init; }

    /// <summary>Destination lane name (or use <see cref="LaneId"/>).</summary>
    public string? LaneName { get; init; }

    /// <summary>Destination 0-based position within the target column/lane.</summary>
    public int? Position { get; init; }
}

/// <summary>Input to assign or reassign a card.</summary>
public sealed record AssignCardInput
{
    /// <summary>The numeric id of the card to assign.</summary>
    public required int CardId { get; init; }

    /// <summary>The user id to set as the primary owner.</summary>
    public int? AssigneeUserId { get; init; }

    /// <summary>When true, set the current user as the primary owner.</summary>
    public bool AssignToMe { get; init; }

    /// <summary>User ids to add as co-owners.</summary>
    public int[]? AddCoOwnerUserIds { get; init; }

    /// <summary>User ids to remove from the co-owners.</summary>
    public int[]? RemoveCoOwnerUserIds { get; init; }
}
