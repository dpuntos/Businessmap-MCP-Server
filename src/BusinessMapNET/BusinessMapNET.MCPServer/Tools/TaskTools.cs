using System.ComponentModel;
using BusinessMapNET.Application.Services;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to manage the checklist (subtasks) of a card. These tools are thin
/// adapters over <see cref="ITaskService"/>: they translate MCP parameters into service calls and
/// map the results back into serializable DTOs.
/// </summary>
[McpServerToolType]
public sealed class TaskTools
{
    private readonly ITaskService _taskService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskTools"/> class.
    /// </summary>
    public TaskTools(ITaskService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
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
    public Task<SubtaskInfo> CreateTaskAsync(
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
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var subtask = await _taskService
                .CreateTaskAsync(cardId, description, assigneeUserId, assignToMe, deadline, cancellationToken)
                .ConfigureAwait(false);
            return DtoMapper.ToInfo(subtask);
        });

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
    public Task<SubtaskInfo> CompleteTaskAsync(
        [Description("The id of the card that owns the subtask.")]
        int cardId,
        [Description("The id of the subtask to update.")]
        int subtaskId,
        [Description("True to mark the subtask as completed (default), false to reopen it.")]
        bool completed = true,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var subtask = await _taskService
                .SetTaskCompletionAsync(cardId, subtaskId, completed, cancellationToken)
                .ConfigureAwait(false);
            return DtoMapper.ToInfo(subtask);
        });
}
