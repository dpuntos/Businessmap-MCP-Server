using BusinessMapNET.Application.Models;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Services;

/// <inheritdoc />
public sealed class WorkflowService : IWorkflowService
{
    private readonly BusinessMapContext _context;
    private readonly ILogger<WorkflowService> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkflowService"/> class.</summary>
    public WorkflowService(BusinessMapContext context, ILogger<WorkflowService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BoardStructure> GetWorkflowAsync(
        int? boardId,
        string? boardName,
        CancellationToken cancellationToken = default)
    {
        var board = await _context.ResolveBoardAsync(boardId, boardName, cancellationToken).ConfigureAwait(false);
        var structure = await _context.GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "get_workflow board={BoardId} columns={Columns} lanes={Lanes}",
            board.BoardId,
            structure.Columns.Count,
            structure.Lanes.Count);

        return structure;
    }
}
