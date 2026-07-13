using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Application.Models;

/// <summary>
/// The result of a card search, carrying the matched cards for the requested page plus
/// pagination metadata computed by the service.
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
public sealed record CardSearchResult(
    IReadOnlyList<Card> Cards,
    int Page,
    int PageSize,
    int Returned,
    int? TotalMatches,
    bool HasMore,
    string? Note);

/// <summary>The number of cards currently in a specific column.</summary>
public sealed record ColumnCardCount(int? ColumnId, string? ColumnName, int Count);

/// <summary>The number of cards assigned to a specific user.</summary>
public sealed record OwnerCardCount(int OwnerUserId, int Count);

/// <summary>An operational summary of a board.</summary>
public sealed record BoardStatusReport(
    int BoardId,
    string? BoardName,
    int TotalCards,
    int Analyzed,
    bool Truncated,
    int BlockedCards,
    int OverdueCards,
    int UnassignedCards,
    IReadOnlyList<ColumnCardCount> Columns,
    IReadOnlyList<OwnerCardCount> Owners);

/// <summary>
/// The result of a create/update/move/assign operation on a card.
/// </summary>
public sealed record CardActionOutcome(
    int CardId,
    string? Title,
    int BoardId,
    int? ColumnId,
    int? LaneId,
    string Message);

/// <summary>The full detail of a card, aggregating the card with its comments and subtasks.</summary>
public sealed record CardDetail(
    Card Card,
    string? BoardName,
    IReadOnlyList<CardComment> Comments,
    IReadOnlyList<CardSubtask> Subtasks);

/// <summary>A card type value pair used to set custom fields on a card.</summary>
public sealed record CustomFieldValue(int FieldId, string? Value);
