using System.ComponentModel;
using BusinessMapNET.Core.Models;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to manage the checklist (subtasks) of a card.
/// </summary>
[McpServerToolType]
public sealed class TaskTools
{
    private readonly BusinessMapToolContext _context;
    private readonly ILogger<TaskTools> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskTools"/> class.
    /// </summary>
    public TaskTools(BusinessMapToolContext context, ILogger<TaskTools> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a subtask (checklist item) inside a card.
    /// </summary>
    /// <remarks>
    /// Example: <c>create_task(cardId: 12345, description: "Write unit tests", assignToMe: true)</c>.
    /// </remarks>
    [McpServerTool(Name = "create_task")]
    [Description(
        "Add a subtask (checklist item) to a card. Optionally assign it to a user (by id or to the current user) " +
        "and set a deadline. Returns the created subtask. Use 'complete_task' to mark it done later.")]
    public async Task<SubtaskInfo> CreateTaskAsync(
        [Description("The id of the card to add the checklist item to.")]
        int cardId,
        [Description("The description/text of the checklist item. Required and non-empty.")]
        string description,
        [Description("The user id to assign the subtask to.")]
        int? assigneeUserId = null,
        [Description("When true, assign the subtask to the currently authenticated user.")]
        bool assignToMe = false,
        [Description("Optional deadline for the subtask in ISO 8601 format.")]
        string? deadline = null,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new ToolException("'cardId' must be a positive card id.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ToolException("'description' is required and cannot be empty.");
        }

        var deadlineValue = NormalizeDeadline(deadline);
        int? ownerId = assigneeUserId;
        if (assignToMe)
        {
            var me = await _context.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
            if (ownerId is not null && ownerId != me)
            {
                throw new ToolException(
                    "Specify either 'assigneeUserId' or 'assignToMe', not both with different users.");
            }

            ownerId = me;
        }

        var request = new CreateSubtaskRequest
        {
            Description = description.Trim(),
            OwnerUserId = ownerId,
            Deadline = deadlineValue,
        };

        _logger.LogInformation("create_task card={CardId}", cardId);

        var subtask = await _context.ExecuteAsync(
            () => _context.Client.Cards.AddCardSubtaskAsync(cardId, request, cancellationToken),
            $"create a subtask on card {cardId}").ConfigureAwait(false);

        return BusinessMapToolContext.ToInfo(subtask);
    }

    /// <summary>
    /// Marks a subtask as completed (or reopens it).
    /// </summary>
    /// <remarks>
    /// Example: <c>complete_task(cardId: 12345, subtaskId: 678)</c>.
    /// </remarks>
    [McpServerTool(Name = "complete_task")]
    [Description(
        "Mark a subtask (checklist item) of a card as completed. Set 'completed' to false to reopen it instead. " +
        "Requires both the card id and the subtask id (obtain subtask ids from 'get_card_details').")]
    public async Task<SubtaskInfo> CompleteTaskAsync(
        [Description("The id of the card that owns the subtask.")]
        int cardId,
        [Description("The id of the subtask to update.")]
        int subtaskId,
        [Description("True to mark the subtask as completed (default), false to reopen it.")]
        bool completed = true,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new ToolException("'cardId' must be a positive card id.");
        }

        if (subtaskId <= 0)
        {
            throw new ToolException("'subtaskId' must be a positive subtask id.");
        }

        var request = new UpdateSubtaskRequest
        {
            IsFinished = completed ? 1 : 0,
        };

        _logger.LogInformation("complete_task card={CardId} subtask={SubtaskId} completed={Completed}",
            cardId, subtaskId, completed);

        var subtask = await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardSubtaskAsync(cardId, subtaskId, request, cancellationToken),
            $"update subtask {subtaskId} of card {cardId}").ConfigureAwait(false);

        return BusinessMapToolContext.ToInfo(subtask);
    }

    private static string? NormalizeDeadline(string? deadline)
    {
        if (string.IsNullOrWhiteSpace(deadline))
        {
            return null;
        }

        return BusinessMapToolContext.ParseDate(deadline) is not null
            ? deadline.Trim()
            : throw new ToolException($"'deadline' is not a valid ISO 8601 date/time: '{deadline}'.");
    }
}
