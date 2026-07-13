using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// Represents a value to set for a card custom field.
/// </summary>
public sealed class CardCustomFieldValue
{
    /// <summary>The identifier of the custom field.</summary>
    [JsonPropertyName("field_id")]
    public int FieldId { get; set; }

    /// <summary>The value to set for the custom field, serialized as text.</summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// Request payload used to create a card. Only the most commonly used fields are exposed.
/// At least a target <see cref="ColumnId"/> and <see cref="LaneId"/> (or a template) are typically required.
/// </summary>
public sealed class CreateCardRequest
{
    /// <summary>The identifier of the target column.</summary>
    [JsonPropertyName("column_id")]
    public int? ColumnId { get; set; }

    /// <summary>The identifier of the target lane.</summary>
    [JsonPropertyName("lane_id")]
    public int? LaneId { get; set; }

    /// <summary>The position of the card within its column/lane.</summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }

    /// <summary>The identifier of a card template to base the new card on.</summary>
    [JsonPropertyName("template_id")]
    public int? TemplateId { get; set; }

    /// <summary>The title of the card.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>The description of the card.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The user-defined custom identifier of the card.</summary>
    [JsonPropertyName("custom_id")]
    public string? CustomId { get; set; }

    /// <summary>The identifier of the owner user.</summary>
    [JsonPropertyName("owner_user_id")]
    public int? OwnerUserId { get; set; }

    /// <summary>The identifier of the card type.</summary>
    [JsonPropertyName("type_id")]
    public int? TypeId { get; set; }

    /// <summary>The size (estimate) of the card.</summary>
    [JsonPropertyName("size")]
    public decimal? Size { get; set; }

    /// <summary>The priority of the card.</summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>The color of the card, as a hex string.</summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>The deadline of the card in ISO 8601 format.</summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>Tag identifiers to add to the card.</summary>
    [JsonPropertyName("tag_ids_to_add")]
    public IEnumerable<int>? TagIdsToAdd { get; set; }

    /// <summary>Co-owner identifiers to add to the card.</summary>
    [JsonPropertyName("co_owner_ids_to_add")]
    public IEnumerable<int>? CoOwnerIdsToAdd { get; set; }

    /// <summary>Watcher identifiers to add to the card.</summary>
    [JsonPropertyName("watcher_ids_to_add")]
    public IEnumerable<int>? WatcherIdsToAdd { get; set; }

    /// <summary>Custom field values to set on the card.</summary>
    [JsonPropertyName("custom_fields_to_add_or_update")]
    public IEnumerable<CardCustomFieldValue>? CustomFieldsToAddOrUpdate { get; set; }
}

/// <summary>
/// Request payload used to update a card. Only the most commonly used fields are exposed.
/// </summary>
public sealed class UpdateCardRequest
{
    /// <summary>The new title of the card.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>The new description of the card.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The identifier of the target column to move the card to.</summary>
    [JsonPropertyName("column_id")]
    public int? ColumnId { get; set; }

    /// <summary>The identifier of the target lane to move the card to.</summary>
    [JsonPropertyName("lane_id")]
    public int? LaneId { get; set; }

    /// <summary>The new position of the card within its column/lane.</summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }

    /// <summary>The identifier of the owner user.</summary>
    [JsonPropertyName("owner_user_id")]
    public int? OwnerUserId { get; set; }

    /// <summary>The identifier of the card type.</summary>
    [JsonPropertyName("type_id")]
    public int? TypeId { get; set; }

    /// <summary>The size (estimate) of the card.</summary>
    [JsonPropertyName("size")]
    public decimal? Size { get; set; }

    /// <summary>The priority of the card.</summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>The color of the card, as a hex string.</summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>The deadline of the card in ISO 8601 format.</summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>Whether the card should be blocked (<c>1</c>) or not (<c>0</c>).</summary>
    [JsonPropertyName("is_blocked")]
    public int? IsBlocked { get; set; }

    /// <summary>Tag identifiers to add to the card.</summary>
    [JsonPropertyName("tag_ids_to_add")]
    public IEnumerable<int>? TagIdsToAdd { get; set; }

    /// <summary>Tag identifiers to remove from the card.</summary>
    [JsonPropertyName("tag_ids_to_remove")]
    public IEnumerable<int>? TagIdsToRemove { get; set; }

    /// <summary>Co-owner identifiers to add to the card.</summary>
    [JsonPropertyName("co_owner_ids_to_add")]
    public IEnumerable<int>? CoOwnerIdsToAdd { get; set; }

    /// <summary>Co-owner identifiers to remove from the card.</summary>
    [JsonPropertyName("co_owner_ids_to_remove")]
    public IEnumerable<int>? CoOwnerIdsToRemove { get; set; }

    /// <summary>Custom field values to set on the card.</summary>
    [JsonPropertyName("custom_fields_to_add_or_update")]
    public IEnumerable<CardCustomFieldValue>? CustomFieldsToAddOrUpdate { get; set; }
}
