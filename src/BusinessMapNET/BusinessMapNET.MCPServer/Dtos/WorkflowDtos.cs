namespace BusinessMapNET.MCPServer.Dtos;

/// <summary>
/// A workflow within a board.
/// </summary>
/// <param name="WorkflowId">The identifier of the workflow.</param>
/// <param name="Name">The name of the workflow.</param>
/// <param name="Type">The type of the workflow, if available.</param>
/// <param name="Position">The position of the workflow within the board.</param>
public sealed record WorkflowSummary(int WorkflowId, string? Name, int? Type, int? Position);

/// <summary>
/// A column within a board workflow.
/// </summary>
/// <param name="ColumnId">The identifier of the column.</param>
/// <param name="Name">The name of the column.</param>
/// <param name="WorkflowId">The identifier of the workflow the column belongs to.</param>
/// <param name="ParentColumnId">The identifier of the parent column, when the column is a sub-column.</param>
/// <param name="Position">The position of the column within its workflow.</param>
/// <param name="Section">The lifecycle section (1 = backlog/requested, 2 = in progress, 3 = done), if available.</param>
/// <param name="Type">The type of the column, if available.</param>
public sealed record ColumnInfo(
    int ColumnId,
    string? Name,
    int? WorkflowId,
    int? ParentColumnId,
    int? Position,
    int? Section,
    string? Type);

/// <summary>
/// A lane (horizontal swimlane) within a board workflow.
/// </summary>
/// <param name="LaneId">The identifier of the lane.</param>
/// <param name="Name">The name of the lane.</param>
/// <param name="WorkflowId">The identifier of the workflow the lane belongs to.</param>
/// <param name="Position">The position of the lane within its workflow.</param>
/// <param name="Color">The color of the lane, as a hex string.</param>
public sealed record LaneInfo(int LaneId, string? Name, int? WorkflowId, int? Position, string? Color);

/// <summary>
/// A card type available on a board.
/// </summary>
/// <param name="TypeId">The identifier of the card type.</param>
/// <param name="Name">The name of the card type.</param>
/// <param name="Color">The color of the card type, as a hex string.</param>
public sealed record CardTypeInfo(int TypeId, string? Name, string? Color);

/// <summary>
/// The useful structure of a board: its workflows, columns, lanes and card types.
/// Use this to discover valid destinations (column/lane ids) and valid card types before
/// creating or moving cards.
/// </summary>
/// <param name="BoardId">The identifier of the board.</param>
/// <param name="BoardName">The name of the board.</param>
/// <param name="Workflows">The workflows defined on the board.</param>
/// <param name="Columns">The columns defined on the board.</param>
/// <param name="Lanes">The lanes defined on the board.</param>
/// <param name="CardTypes">The card types available on the board.</param>
public sealed record WorkflowInfo(
    int BoardId,
    string? BoardName,
    IReadOnlyList<WorkflowSummary> Workflows,
    IReadOnlyList<ColumnInfo> Columns,
    IReadOnlyList<LaneInfo> Lanes,
    IReadOnlyList<CardTypeInfo> CardTypes);
