namespace BusinessMapNET.MCPServer.Dtos;

/// <summary>
/// A lightweight projection of a board, with the minimum information needed to choose one.
/// </summary>
/// <param name="BoardId">The unique identifier of the board.</param>
/// <param name="Name">The name of the board.</param>
/// <param name="Description">The description of the board, if any.</param>
/// <param name="WorkspaceId">The identifier of the workspace the board belongs to.</param>
/// <param name="WorkspaceName">The name of the workspace the board belongs to, if resolvable.</param>
/// <param name="Type">The type of the board (1 = Kanban board, 2 = AI Canvas).</param>
/// <param name="IsArchived">Whether the board is archived.</param>
public sealed record BoardSummary(
    int BoardId,
    string? Name,
    string? Description,
    int WorkspaceId,
    string? WorkspaceName,
    int Type,
    bool IsArchived);

/// <summary>
/// The number of cards currently in a specific column.
/// </summary>
/// <param name="ColumnId">The identifier of the column.</param>
/// <param name="ColumnName">The name of the column, if resolvable.</param>
/// <param name="Count">The number of cards in the column.</param>
public sealed record ColumnCardCount(int? ColumnId, string? ColumnName, int Count);

/// <summary>
/// The number of cards assigned to a specific user.
/// </summary>
/// <param name="OwnerUserId">The identifier of the owner user.</param>
/// <param name="Count">The number of cards assigned to the user.</param>
public sealed record OwnerCardCount(int OwnerUserId, int Count);

/// <summary>
/// An operational summary of a board.
/// </summary>
/// <param name="BoardId">The identifier of the board.</param>
/// <param name="BoardName">The name of the board.</param>
/// <param name="TotalCards">The total number of active cards analyzed.</param>
/// <param name="Analyzed">The number of cards actually inspected (may be capped for very large boards).</param>
/// <param name="Truncated">Whether analysis was capped and some cards were not inspected.</param>
/// <param name="BlockedCards">The number of blocked cards.</param>
/// <param name="OverdueCards">The number of cards whose deadline is in the past and are not in a done column.</param>
/// <param name="UnassignedCards">The number of cards without an owner.</param>
/// <param name="Columns">The per-column card counts.</param>
/// <param name="Owners">The per-owner card counts, ordered by count descending.</param>
public sealed record BoardStatusResult(
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
