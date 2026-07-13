using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// Represents a Businessmap card. Only the most commonly used fields are strongly typed;
/// richer nested structures are exposed as raw <see cref="JsonElement"/> values.
/// </summary>
public sealed class Card
{
    /// <summary>The unique identifier of the card.</summary>
    [JsonPropertyName("card_id")]
    public int CardId { get; init; }

    /// <summary>The user-defined custom identifier of the card.</summary>
    [JsonPropertyName("custom_id")]
    public string? CustomId { get; init; }

    /// <summary>The identifier of the board the card belongs to.</summary>
    [JsonPropertyName("board_id")]
    public int BoardId { get; init; }

    /// <summary>The identifier of the workflow the card belongs to.</summary>
    [JsonPropertyName("workflow_id")]
    public int WorkflowId { get; init; }

    /// <summary>The title of the card.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The description of the card.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The identifier of the owner user.</summary>
    [JsonPropertyName("owner_user_id")]
    public int? OwnerUserId { get; init; }

    /// <summary>The identifier of the card type.</summary>
    [JsonPropertyName("type_id")]
    public int? TypeId { get; init; }

    /// <summary>The color of the card, as a hex string.</summary>
    [JsonPropertyName("color")]
    public string? Color { get; init; }

    /// <summary>The identifier of the column the card is currently in.</summary>
    [JsonPropertyName("column_id")]
    public int? ColumnId { get; init; }

    /// <summary>The identifier of the lane the card is currently in.</summary>
    [JsonPropertyName("lane_id")]
    public int? LaneId { get; init; }

    /// <summary>The position of the card within its column/lane.</summary>
    [JsonPropertyName("position")]
    public int? Position { get; init; }

    /// <summary>The size (estimate) of the card.</summary>
    [JsonPropertyName("size")]
    public decimal? Size { get; init; }

    /// <summary>The priority of the card.</summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; init; }

    /// <summary>The deadline of the card in ISO 8601 format.</summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; init; }

    /// <summary>Whether the card is blocked (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_blocked")]
    public int IsBlocked { get; init; }

    /// <summary>The date/time the card was created, in ISO 8601 format.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    /// <summary>The date/time the card was last modified, in ISO 8601 format.</summary>
    [JsonPropertyName("last_modified")]
    public string? LastModified { get; init; }

    /// <summary>The current structure revision of the card.</summary>
    [JsonPropertyName("revision")]
    public int Revision { get; init; }

    /// <summary>The identifiers of the tags attached to the card.</summary>
    [JsonPropertyName("tag_ids")]
    public IReadOnlyList<int>? TagIds { get; init; }

    /// <summary>The identifiers of the co-owners of the card.</summary>
    [JsonPropertyName("co_owner_ids")]
    public IReadOnlyList<int>? CoOwnerIds { get; init; }

    /// <summary>The identifiers of the watchers of the card.</summary>
    [JsonPropertyName("watcher_ids")]
    public IReadOnlyList<int>? WatcherIds { get; init; }

    /// <summary>The identifiers of the milestones attached to the card.</summary>
    [JsonPropertyName("milestone_ids")]
    public IReadOnlyList<int>? MilestoneIds { get; init; }

    /// <summary>The custom fields attached to the card, as raw JSON.</summary>
    [JsonPropertyName("custom_fields")]
    public JsonElement? CustomFields { get; init; }

    /// <summary>The stickers attached to the card, as raw JSON.</summary>
    [JsonPropertyName("stickers")]
    public JsonElement? Stickers { get; init; }

    /// <summary>The subtasks of the card, as raw JSON.</summary>
    [JsonPropertyName("subtasks")]
    public JsonElement? Subtasks { get; init; }
}
