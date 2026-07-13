namespace BusinessMapNET.MCPServer.Dtos;

/// <summary>
/// A lightweight projection of a card, returned by list/search operations.
/// </summary>
/// <param name="CardId">The unique identifier of the card.</param>
/// <param name="CustomId">The user-defined custom identifier of the card, if any.</param>
/// <param name="Title">The title of the card.</param>
/// <param name="BoardId">The identifier of the board the card belongs to.</param>
/// <param name="WorkflowId">The identifier of the workflow the card belongs to.</param>
/// <param name="ColumnId">The identifier of the column the card is currently in.</param>
/// <param name="LaneId">The identifier of the lane the card is currently in.</param>
/// <param name="Position">The position of the card within its column/lane.</param>
/// <param name="Priority">The numeric priority of the card (higher usually means more urgent).</param>
/// <param name="OwnerUserId">The identifier of the assignee (owner) user, if any.</param>
/// <param name="Deadline">The deadline of the card in ISO 8601 format, if any.</param>
/// <param name="IsBlocked">Whether the card is currently blocked.</param>
/// <param name="Size">The size/estimate of the card, if any.</param>
/// <param name="TypeId">The identifier of the card type, if any.</param>
/// <param name="TagIds">The identifiers of the tags attached to the card.</param>
public sealed record CardSummary(
    int CardId,
    string? CustomId,
    string? Title,
    int BoardId,
    int WorkflowId,
    int? ColumnId,
    int? LaneId,
    int? Position,
    int? Priority,
    int? OwnerUserId,
    string? Deadline,
    bool IsBlocked,
    decimal? Size,
    int? TypeId,
    IReadOnlyList<int>? TagIds);

/// <summary>
/// A single custom field value attached to a card.
/// </summary>
/// <param name="FieldId">The identifier of the custom field.</param>
/// <param name="Value">The value of the custom field, rendered as text.</param>
public sealed record CustomFieldValueInfo(int FieldId, string? Value);

/// <summary>
/// The result of a create/update/move/assign operation on a card.
/// </summary>
/// <param name="CardId">The identifier of the affected card.</param>
/// <param name="Title">The title of the card after the operation.</param>
/// <param name="BoardId">The identifier of the board the card belongs to.</param>
/// <param name="ColumnId">The identifier of the column the card is in after the operation.</param>
/// <param name="LaneId">The identifier of the lane the card is in after the operation.</param>
/// <param name="Message">A human-readable summary of what happened.</param>
public sealed record CardActionResult(
    int CardId,
    string? Title,
    int BoardId,
    int? ColumnId,
    int? LaneId,
    string Message);

/// <summary>
/// The result of a <c>find_cards</c> search, including pagination metadata.
/// </summary>
/// <param name="Cards">The matching cards for the requested page.</param>
/// <param name="Page">The 1-based page number returned.</param>
/// <param name="PageSize">The maximum number of cards per page.</param>
/// <param name="Returned">The number of cards actually returned in this page.</param>
/// <param name="TotalMatches">
/// The total number of matches when it can be computed locally (i.e. when client-side filters were applied);
/// <see langword="null"/> when only server-side paging was used and the total is unknown.
/// </param>
/// <param name="HasMore">Whether more results are likely available beyond this page.</param>
/// <param name="Note">An optional note explaining how the search was resolved or filtered.</param>
public sealed record FindCardsResult(
    IReadOnlyList<CardSummary> Cards,
    int Page,
    int PageSize,
    int Returned,
    int? TotalMatches,
    bool HasMore,
    string? Note);

/// <summary>
/// The full detail view of a card, aggregating basic data, comments, subtasks and custom fields.
/// </summary>
/// <param name="CardId">The unique identifier of the card.</param>
/// <param name="CustomId">The user-defined custom identifier of the card, if any.</param>
/// <param name="Title">The title of the card.</param>
/// <param name="Description">The description of the card.</param>
/// <param name="BoardId">The identifier of the board the card belongs to.</param>
/// <param name="BoardName">The name of the board the card belongs to, if resolvable.</param>
/// <param name="WorkflowId">The identifier of the workflow the card belongs to.</param>
/// <param name="ColumnId">The identifier of the column the card is currently in.</param>
/// <param name="LaneId">The identifier of the lane the card is currently in.</param>
/// <param name="Position">The position of the card within its column/lane.</param>
/// <param name="Priority">The numeric priority of the card.</param>
/// <param name="OwnerUserId">The identifier of the assignee (owner) user, if any.</param>
/// <param name="CoOwnerIds">The identifiers of the co-owners (secondary assignees) of the card.</param>
/// <param name="WatcherIds">The identifiers of the watchers of the card.</param>
/// <param name="TypeId">The identifier of the card type, if any.</param>
/// <param name="Size">The size/estimate of the card, if any.</param>
/// <param name="Color">The color of the card, as a hex string.</param>
/// <param name="Deadline">The deadline of the card in ISO 8601 format, if any.</param>
/// <param name="IsBlocked">Whether the card is currently blocked.</param>
/// <param name="TagIds">The identifiers of the tags attached to the card.</param>
/// <param name="CreatedAt">The date/time the card was created, in ISO 8601 format.</param>
/// <param name="LastModified">The date/time the card was last modified, in ISO 8601 format.</param>
/// <param name="Comments">The comments attached to the card.</param>
/// <param name="Subtasks">The subtasks (checklist items) of the card.</param>
/// <param name="CustomFields">The custom field values attached to the card.</param>
public sealed record CardDetails(
    int CardId,
    string? CustomId,
    string? Title,
    string? Description,
    int BoardId,
    string? BoardName,
    int WorkflowId,
    int? ColumnId,
    int? LaneId,
    int? Position,
    int? Priority,
    int? OwnerUserId,
    IReadOnlyList<int>? CoOwnerIds,
    IReadOnlyList<int>? WatcherIds,
    int? TypeId,
    decimal? Size,
    string? Color,
    string? Deadline,
    bool IsBlocked,
    IReadOnlyList<int>? TagIds,
    string? CreatedAt,
    string? LastModified,
    IReadOnlyList<CommentInfo> Comments,
    IReadOnlyList<SubtaskInfo> Subtasks,
    IReadOnlyList<CustomFieldValueInfo> CustomFields);
