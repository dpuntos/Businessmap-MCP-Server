using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// Represents a comment attached to a card.
/// </summary>
public sealed class CardComment
{
    /// <summary>The unique identifier of the comment.</summary>
    [JsonPropertyName("comment_id")]
    public int CommentId { get; init; }

    /// <summary>The type of the comment.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>The text content of the comment.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>The date/time the comment was created, in ISO 8601 format.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    /// <summary>The date/time the comment was last modified, in ISO 8601 format.</summary>
    [JsonPropertyName("last_modified")]
    public string? LastModified { get; init; }

    /// <summary>The attachments of the comment, as raw JSON.</summary>
    [JsonPropertyName("attachments")]
    public JsonElement? Attachments { get; init; }
}

/// <summary>
/// Request payload used to add a comment to a card.
/// </summary>
public sealed class CreateCommentRequest
{
    /// <summary>The text content of the comment. Required.</summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
