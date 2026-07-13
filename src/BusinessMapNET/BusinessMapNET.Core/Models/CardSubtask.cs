using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// Represents a subtask of a card.
/// </summary>
public sealed class CardSubtask
{
    /// <summary>The unique identifier of the subtask.</summary>
    [JsonPropertyName("subtask_id")]
    public int SubtaskId { get; init; }

    /// <summary>The description of the subtask.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The identifier of the owner user.</summary>
    [JsonPropertyName("owner_user_id")]
    public int? OwnerUserId { get; init; }

    /// <summary>The deadline of the subtask in ISO 8601 format.</summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; init; }

    /// <summary>The date/time the subtask was finished, in ISO 8601 format, if finished.</summary>
    [JsonPropertyName("finished_at")]
    public string? FinishedAt { get; init; }

    /// <summary>The date/time the subtask was created, in ISO 8601 format.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    /// <summary>The position of the subtask within the card.</summary>
    [JsonPropertyName("position")]
    public int? Position { get; init; }

    /// <summary>The attachments of the subtask, as raw JSON.</summary>
    [JsonPropertyName("attachments")]
    public JsonElement? Attachments { get; init; }
}

/// <summary>
/// Request payload used to add a subtask to a card.
/// </summary>
public sealed class CreateSubtaskRequest
{
    /// <summary>The description of the subtask. Required.</summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    /// <summary>The identifier of the owner user.</summary>
    [JsonPropertyName("owner_user_id")]
    public int? OwnerUserId { get; set; }

    /// <summary>Whether the subtask is finished (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_finished")]
    public int? IsFinished { get; set; }

    /// <summary>The deadline of the subtask in ISO 8601 format.</summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>The position of the subtask within the card.</summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }
}

/// <summary>
/// Request payload used to update a subtask. Only non-null properties are sent.
/// </summary>
public sealed class UpdateSubtaskRequest
{
    /// <summary>The new description of the subtask.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The identifier of the owner user.</summary>
    [JsonPropertyName("owner_user_id")]
    public int? OwnerUserId { get; set; }

    /// <summary>Whether the subtask is finished (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_finished")]
    public int? IsFinished { get; set; }

    /// <summary>The deadline of the subtask in ISO 8601 format.</summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>The new position of the subtask within the card.</summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }
}
