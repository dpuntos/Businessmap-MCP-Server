using System.ComponentModel;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tool to inspect the useful structure of a board (workflows, columns, lanes, card types).
/// </summary>
[McpServerToolType]
public sealed class WorkflowTools
{
    private readonly BusinessMapToolContext _context;
    private readonly ILogger<WorkflowTools> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTools"/> class.
    /// </summary>
    public WorkflowTools(BusinessMapToolContext context, ILogger<WorkflowTools> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    public async Task<WorkflowInfo> GetWorkflowAsync(
        [Description("The board id. Provide this or 'boardName'.")]
        int? boardId = null,
        [Description("The board name (case-insensitive). Provide this or 'boardId'.")]
        string? boardName = null,
        CancellationToken cancellationToken = default)
    {
        var board = await _context.ResolveBoardAsync(boardId, boardName, cancellationToken).ConfigureAwait(false);
        var structure = await _context.GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "get_workflow board={BoardId} columns={Columns} lanes={Lanes}",
            board.BoardId,
            structure.Columns.Count,
            structure.Lanes.Count);

        return structure.ToWorkflowInfo();
    }
}
