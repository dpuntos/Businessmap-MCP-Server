namespace BusinessMapNET.MCPServer.Dtos;

/// <summary>
/// A comment attached to a card.
/// </summary>
/// <param name="CommentId">The unique identifier of the comment.</param>
/// <param name="Type">The type of the comment.</param>
/// <param name="Text">The text content of the comment.</param>
/// <param name="CreatedAt">The date/time the comment was created, in ISO 8601 format.</param>
/// <param name="LastModified">The date/time the comment was last modified, in ISO 8601 format.</param>
public sealed record CommentInfo(
    int CommentId,
    string? Type,
    string? Text,
    string? CreatedAt,
    string? LastModified);

/// <summary>
/// A subtask (checklist item) of a card.
/// </summary>
/// <param name="SubtaskId">The unique identifier of the subtask.</param>
/// <param name="Description">The description of the subtask.</param>
/// <param name="IsFinished">Whether the subtask has been completed.</param>
/// <param name="OwnerUserId">The identifier of the owner user, if any.</param>
/// <param name="Deadline">The deadline of the subtask in ISO 8601 format, if any.</param>
/// <param name="FinishedAt">The date/time the subtask was finished, in ISO 8601 format, if finished.</param>
/// <param name="Position">The position of the subtask within the card.</param>
public sealed record SubtaskInfo(
    int SubtaskId,
    string? Description,
    bool IsFinished,
    int? OwnerUserId,
    string? Deadline,
    string? FinishedAt,
    int? Position);
