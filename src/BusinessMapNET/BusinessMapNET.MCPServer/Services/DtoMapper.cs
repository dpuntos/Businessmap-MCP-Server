using System.Text.Json;
using BusinessMapNET.Application.Models;
using BusinessMapNET.Core.Models;
using BusinessMapNET.MCPServer.Dtos;

namespace BusinessMapNET.MCPServer.Services;

/// <summary>
/// Maps Core domain models and Application service results into the serializable DTOs exposed
/// through the MCP protocol. Keeping this in the MCP layer means the DTO contract stays a concern
/// of the protocol host, not of the business layer.
/// </summary>
internal static class DtoMapper
{
    public static CardSummary ToSummary(Card card) => new(
        card.CardId,
        card.CustomId,
        card.Title,
        card.BoardId,
        card.WorkflowId,
        card.ColumnId,
        card.LaneId,
        card.Position,
        card.Priority,
        card.OwnerUserId,
        card.Deadline,
        card.IsBlocked == 1,
        card.Size,
        card.TypeId,
        card.TagIds);

    public static CommentInfo ToInfo(CardComment comment) => new(
        comment.CommentId,
        comment.Type,
        comment.Text,
        comment.CreatedAt,
        comment.LastModified);

    public static SubtaskInfo ToInfo(CardSubtask subtask) => new(
        subtask.SubtaskId,
        subtask.Description,
        !string.IsNullOrEmpty(subtask.FinishedAt),
        subtask.OwnerUserId,
        subtask.Deadline,
        subtask.FinishedAt,
        subtask.Position);

    public static UserSummary ToSummary(User user) => new(
        user.UserId,
        user.RealName,
        user.Username,
        user.Email,
        user.IsEnabled == 1);

    public static BoardSummary ToSummary(Board board, IReadOnlyDictionary<int, string?>? workspaceNames) => new(
        board.BoardId,
        board.Name,
        board.Description,
        board.WorkspaceId,
        workspaceNames?.GetValueOrDefault(board.WorkspaceId),
        board.Type,
        board.IsArchived == 1);

    public static CardActionResult ToDto(CardActionOutcome outcome) => new(
        outcome.CardId,
        outcome.Title,
        outcome.BoardId,
        outcome.ColumnId,
        outcome.LaneId,
        outcome.Message);

    public static FindCardsResult ToDto(CardSearchResult result) => new(
        result.Cards.Select(ToSummary).ToList(),
        result.Page,
        result.PageSize,
        result.Returned,
        result.TotalMatches,
        result.HasMore,
        result.Note);

    public static CardDetails ToDto(CardDetail detail)
    {
        var card = detail.Card;
        return new CardDetails(
            card.CardId,
            card.CustomId,
            card.Title,
            card.Description,
            card.BoardId,
            detail.BoardName,
            card.WorkflowId,
            card.ColumnId,
            card.LaneId,
            card.Position,
            card.Priority,
            card.OwnerUserId,
            card.CoOwnerIds,
            card.WatcherIds,
            card.TypeId,
            card.Size,
            card.Color,
            card.Deadline,
            card.IsBlocked == 1,
            card.TagIds,
            card.CreatedAt,
            card.LastModified,
            detail.Comments.Select(ToInfo).ToList(),
            detail.Subtasks.Select(ToInfo).ToList(),
            ReadCustomFields(card.CustomFields));
    }

    public static BoardStatusResult ToDto(BoardStatusReport report) => new(
        report.BoardId,
        report.BoardName,
        report.TotalCards,
        report.Analyzed,
        report.Truncated,
        report.BlockedCards,
        report.OverdueCards,
        report.UnassignedCards,
        report.Columns.Select(c => new Dtos.ColumnCardCount(c.ColumnId, c.ColumnName, c.Count)).ToList(),
        report.Owners.Select(o => new Dtos.OwnerCardCount(o.OwnerUserId, o.Count)).ToList());

    public static WorkflowInfo ToWorkflowInfo(BoardStructure structure) => new(
        structure.BoardId,
        structure.BoardName,
        structure.Workflows.Select(w => new WorkflowSummary(w.WorkflowId, w.Name, w.Type, w.Position)).ToList(),
        structure.Columns
            .Select(c => new ColumnInfo(c.ColumnId, c.Name, c.WorkflowId, c.ParentColumnId, c.Position, c.Section, c.Type))
            .ToList(),
        structure.Lanes.Select(l => new LaneInfo(l.LaneId, l.Name, l.WorkflowId, l.Position, l.Color)).ToList(),
        structure.CardTypes.Select(t => new CardTypeInfo(t.TypeId, t.Name, t.Color)).ToList());

    public static IReadOnlyList<CustomFieldValueInfo> ReadCustomFields(JsonElement? customFields)
    {
        var results = new List<CustomFieldValueInfo>();
        if (customFields is not { } element)
        {
            return results;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (TryReadCustomField(item, out var info))
                    {
                        results.Add(info);
                    }
                }
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object &&
                        TryReadCustomField(property.Value, out var info))
                    {
                        results.Add(info);
                    }
                }
                break;
        }

        return results;
    }

    private static bool TryReadCustomField(JsonElement element, out CustomFieldValueInfo info)
    {
        info = default!;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("field_id", out var fieldIdElement) ||
            !fieldIdElement.TryGetInt32(out var fieldId))
        {
            return false;
        }

        string? value = null;
        if (element.TryGetProperty("value", out var valueElement))
        {
            value = valueElement.ValueKind switch
            {
                JsonValueKind.String => valueElement.GetString(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => valueElement.ToString()
            };
        }

        info = new CustomFieldValueInfo(fieldId, value);
        return true;
    }
}
