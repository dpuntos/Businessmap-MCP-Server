using System.ComponentModel;
using BusinessMapNET.Application.Services;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tool to inspect the useful structure of a board (workflows, columns, lanes, card
/// types). Thin adapter over <see cref="IWorkflowService"/>.
/// </summary>
[McpServerToolType]
public sealed class WorkflowTools
{
    private readonly IWorkflowService _workflowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTools"/> class.
    /// </summary>
    public WorkflowTools(IWorkflowService workflowService)
    {
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
    }

    /// <summary>
    /// Returns the useful structure of a board: workflows, columns, lanes and card types.
    /// </summary>
    /// <remarks>
    /// Example: discover valid columns/lanes before creating or moving a card:
    /// <code>
    /// get_workflow(boardName: "Delivery")
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "get_workflow")]
    [Description(
        "Get the useful structure of a board (by id or name): its workflows, columns (with lifecycle section and " +
        "parent column), lanes and available card types. Use this to discover the valid column/lane ids and card " +
        "type ids needed by 'create_card' and 'move_card', or to explain a board's layout to the user.")]
    public Task<WorkflowInfo> GetWorkflowAsync(
        [Description("The board id. Provide this or 'boardName'.")]
        int? boardId = null,
        [Description("The board name (case-insensitive). Provide this or 'boardId'.")]
        string? boardName = null,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var structure = await _workflowService
                .GetWorkflowAsync(boardId, boardName, cancellationToken)
                .ConfigureAwait(false);
            return DtoMapper.ToWorkflowInfo(structure);
        });
}
