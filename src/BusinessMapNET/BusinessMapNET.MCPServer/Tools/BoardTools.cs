using System.ComponentModel;
using BusinessMapNET.Application.Services;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to discover boards and summarize their operational status. These tools are
/// thin adapters over <see cref="IBoardService"/>: they translate MCP parameters into service calls
/// and map the results back into serializable DTOs.
/// </summary>
[McpServerToolType]
public sealed class BoardTools
{
    private readonly IBoardService _boardService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardTools"/> class.
    /// </summary>
    public BoardTools(IBoardService boardService)
    {
        _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
    }

    /// <summary>
    /// Lists the accessible boards, optionally filtered by a name substring and archived state.
    /// </summary>
    /// <remarks>
    /// Example: <c>list_boards(nameContains: "Delivery")</c>.
    /// </remarks>
    [McpServerTool(Name = "list_boards")]
    [Description(
        "List the boards accessible in the Kanbanize (Businessmap) account, including their id, name, description, " +
        "workspace and archived state. Use this to resolve a board name into the 'boardId' required by other tools, " +
        "or to explain the available boards to the user. Supports an optional case-insensitive name filter.")]
    public Task<IReadOnlyList<BoardSummary>> ListBoardsAsync(
        [Description("Optional case-insensitive substring matched against the board name.")]
        string? nameContains = null,
        [Description("Whether to include archived boards. Defaults to false.")]
        bool includeArchived = false,
        [Description("Maximum number of boards to return (1-500). Defaults to 200.")]
        int limit = 200,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var boards = await _boardService
                .ListBoardsAsync(nameContains, includeArchived, limit, cancellationToken)
                .ConfigureAwait(false);
            var workspaceNames = await _boardService
                .TryGetWorkspaceNamesAsync(cancellationToken)
                .ConfigureAwait(false);
            return (IReadOnlyList<BoardSummary>)boards
                .Select(b => DtoMapper.ToSummary(b, workspaceNames))
                .ToList();
        });

    /// <summary>
    /// Produces an operational snapshot of a board (by id or name).
    /// </summary>
    /// <remarks>
    /// Example: <c>get_board_status(boardName: "Delivery")</c>.
    /// </remarks>
    [McpServerTool(Name = "get_board_status")]
    [Description(
        "Get an operational snapshot of a board (by id or name): total active cards, blocked, overdue and " +
        "unassigned counts, plus per-column and per-owner card counts. Use this to summarize the health or " +
        "workload of a board. Resolve owner ids to names with 'list_users'.")]
    public Task<BoardStatusResult> GetBoardStatusAsync(
        [Description("The board id. Provide this or 'boardName'.")]
        int? boardId = null,
        [Description("The board name (case-insensitive). Provide this or 'boardId'.")]
        string? boardName = null,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var report = await _boardService
                .GetBoardStatusAsync(boardId, boardName, cancellationToken)
                .ConfigureAwait(false);
            return DtoMapper.ToDto(report);
        });
}
