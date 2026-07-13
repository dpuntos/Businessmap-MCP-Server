namespace BusinessMapNET.Application.Models;

/// <summary>A workflow within a board.</summary>
public sealed record WorkflowDefinition(int WorkflowId, string? Name, int? Type, int? Position);

/// <summary>A column within a board workflow.</summary>
public sealed record ColumnDefinition(
    int ColumnId,
    string? Name,
    int? WorkflowId,
    int? ParentColumnId,
    int? Position,
    int? Section,
    string? Type);

/// <summary>A lane (horizontal swimlane) within a board workflow.</summary>
public sealed record LaneDefinition(int LaneId, string? Name, int? WorkflowId, int? Position, string? Color);

/// <summary>A card type available on a board.</summary>
public sealed record CardTypeDefinition(int TypeId, string? Name, string? Color);
