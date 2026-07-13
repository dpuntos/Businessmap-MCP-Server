using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// Represents a Businessmap board.
/// </summary>
public sealed class Board
{
    /// <summary>The unique identifier of the board.</summary>
    [JsonPropertyName("board_id")]
    public int BoardId { get; init; }

    /// <summary>The identifier of the workspace to which the board belongs.</summary>
    [JsonPropertyName("workspace_id")]
    public int WorkspaceId { get; init; }

    /// <summary>The name of the board.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The description of the board.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The type of the board. <c>1</c> = Kanban board, <c>2</c> = AI Canvas.</summary>
    [JsonPropertyName("type")]
    public int Type { get; init; }

    /// <summary>The current structure revision of the board.</summary>
    [JsonPropertyName("revision")]
    public int Revision { get; init; }

    /// <summary>Whether the board is archived (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_archived")]
    public int IsArchived { get; init; }
}

/// <summary>
/// Request payload used to create a board.
/// </summary>
public sealed class CreateBoardRequest
{
    /// <summary>The identifier of the workspace to which the board belongs. Required.</summary>
    [JsonPropertyName("workspace_id")]
    public required int WorkspaceId { get; set; }

    /// <summary>The name of the board. Required.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>The description of the board.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The type of the board. <c>1</c> = Kanban board (default), <c>2</c> = AI Canvas.</summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }
}

/// <summary>
/// Request payload used to update a board.
/// </summary>
public sealed class UpdateBoardRequest
{
    /// <summary>The new name of the board.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The new description of the board.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Whether the board should be archived (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_archived")]
    public int? IsArchived { get; set; }
}
