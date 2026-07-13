using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// Represents a Businessmap workspace.
/// </summary>
public sealed class Workspace
{
    /// <summary>The unique identifier of the workspace.</summary>
    [JsonPropertyName("workspace_id")]
    public int WorkspaceId { get; init; }

    /// <summary>The name of the workspace.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The type of the workspace. <c>1</c> = Team Workspace, <c>2</c> = Management Workspace.
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; init; }

    /// <summary>Whether the workspace is archived (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_archived")]
    public int IsArchived { get; init; }
}

/// <summary>
/// Request payload used to create a workspace.
/// </summary>
public sealed class CreateWorkspaceRequest
{
    /// <summary>The name of the workspace. Required.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The type of the workspace. <c>1</c> = Team Workspace (default), <c>2</c> = Management Workspace.
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }
}

/// <summary>
/// Request payload used to update a workspace.
/// </summary>
public sealed class UpdateWorkspaceRequest
{
    /// <summary>The new name of the workspace.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Whether the workspace should be archived (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_archived")]
    public int? IsArchived { get; set; }
}
