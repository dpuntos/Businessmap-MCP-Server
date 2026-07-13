using System.ComponentModel;
using BusinessMapNET.Core.Models;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to discover boards and get an operational overview of a board.
/// </summary>
[McpServerToolType]
public sealed class BoardTools
{
    private const int MaxScanCards = 1000;
    private const int FetchSize = 100;
    private const int MaxOwnersReturned = 25;

    private readonly BusinessMapToolContext _context;
    private readonly ILogger<BoardTools> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardTools"/> class.
    /// </summary>
    public BoardTools(BusinessMapToolContext context, ILogger<BoardTools> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists the accessible boards with the minimum information needed to choose one.
    /// </summary>
    /// <remarks>
    /// Example: list active boards whose name contains "delivery":
    /// <code>
    /// list_boards(nameContains: "delivery", includeArchived: false)
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "list_boards")]
    [Description(
        "List the boards the API key can access, including board id, name, description and the owning workspace. " +
        "Use this first to discover which board to work on, then pass the chosen board id/name to other tools. " +
        "Supports an optional name filter and a limit.")]
    public async Task<IReadOnlyList<BoardSummary>> ListBoardsAsync(
        [Description("Optional case-insensitive substring to filter boards by name.")]
        string? nameContains = null,
        [Description("Whether to include archived boards. Defaults to false.")]
        bool includeArchived = false,
        [Description("Maximum number of boards to return (1-200). Defaults to 100.")]
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);

        var boards = await _context.GetBoardsAsync(cancellationToken).ConfigureAwait(false);
        var workspaceNames = await TryGetWorkspaceNamesAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<Board> filtered = boards;

        if (!includeArchived)
        {
            filtered = filtered.Where(b => b.IsArchived == 0);
        }

        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            filtered = filtered.Where(b =>
                b.Name is not null && b.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase));
        }

        var result = filtered
            .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(b => new BoardSummary(
                b.BoardId,
                b.Name,
                b.Description,
                b.WorkspaceId,
                workspaceNames?.GetValueOrDefault(b.WorkspaceId),
                b.Type,
                b.IsArchived == 1))
            .ToList();

        _logger.LogInformation("list_boards returned {Count} board(s).", result.Count);
        return result;
    }

    /// <summary>
    /// Produces an operational summary of a board: totals, blocked/overdue/unassigned counts,
    /// per-column counts and per-owner counts.
    /// </summary>
    /// <remarks>
    /// Example: <c>get_board_status(boardName: "Delivery")</c>.
    /// </remarks>
    [McpServerTool(Name = "get_board_status")]
    [Description(
        "Get an operational snapshot of a board (by id or name): total active cards, number blocked, number overdue " +
        "(deadline in the past and not in a done column), number unassigned, card counts per column and card counts " +
        "per owner. Use this to answer questions like 'how is the board doing' or 'who is overloaded'. Analysis is " +
        "capped for very large boards and will indicate when results were truncated.")]
    public async Task<BoardStatusResult> GetBoardStatusAsync(
        [Description("The board id. Provide this or 'boardName'.")]
        int? boardId = null,
        [Description("The board name (case-insensitive). Provide this or 'boardId'.")]
        string? boardName = null,
        CancellationToken cancellationToken = default)
    {
        var board = await _context.ResolveBoardAsync(boardId, boardName, cancellationToken).ConfigureAwait(false);
        var structure = await _context.GetBoardStructureAsync(board, cancellationToken).ConfigureAwait(false);

        var doneColumnIds = structure.Columns
            .Where(c => c.Section == 3)
            .Select(c => c.ColumnId)
            .ToHashSet();

        var columnCounts = new Dictionary<int, int>();
        var ownerCounts = new Dictionary<int, int>();
        var now = DateTimeOffset.UtcNow;

        var total = 0;
        var blocked = 0;
        var overdue = 0;
        var unassigned = 0;
        var scanned = 0;
        var truncated = false;

        var query = new CardsQuery
        {
            BoardIds = [board.BoardId],
            State = CardState.Active,
            PerPage = FetchSize,
        };

        var currentPage = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            query.Page = currentPage;

            var result = await _context.ExecuteAsync(
                () => _context.Client.Cards.GetCardsAsync(query, cancellationToken),
                $"read cards of board {board.BoardId}").ConfigureAwait(false);

            foreach (var card in result.Data)
            {
                scanned++;
                total++;

                var columnKey = card.ColumnId ?? 0;
                columnCounts[columnKey] = columnCounts.GetValueOrDefault(columnKey) + 1;

                if (card.IsBlocked == 1)
                {
                    blocked++;
                }

                if (card.OwnerUserId is int owner && owner > 0)
                {
                    ownerCounts[owner] = ownerCounts.GetValueOrDefault(owner) + 1;
                }
                else
                {
                    unassigned++;
                }

                if (IsOverdue(card, now, doneColumnIds))
                {
                    overdue++;
                }
            }

            var pagination = result.Pagination;
            var moreServerPages = pagination is not null && pagination.CurrentPage < pagination.AllPages;

            if (!moreServerPages || result.Data.Count == 0)
            {
                break;
            }

            if (scanned >= MaxScanCards)
            {
                truncated = true;
                break;
            }

            currentPage++;
        }

        var columns = columnCounts
            .Select(kvp =>
            {
                int? columnId = kvp.Key == 0 ? null : kvp.Key;
                return new ColumnCardCount(columnId, structure.GetColumnName(columnId), kvp.Value);
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        var owners = ownerCounts
            .Select(kvp => new OwnerCardCount(kvp.Key, kvp.Value))
            .OrderByDescending(o => o.Count)
            .Take(MaxOwnersReturned)
            .ToList();

        _logger.LogInformation(
            "get_board_status board={BoardId} total={Total} blocked={Blocked} overdue={Overdue}",
            board.BoardId,
            total,
            blocked,
            overdue);

        return new BoardStatusResult(
            board.BoardId,
            board.Name,
            total,
            scanned,
            truncated,
            blocked,
            overdue,
            unassigned,
            columns,
            owners);
    }

    private static bool IsOverdue(Card card, DateTimeOffset now, HashSet<int> doneColumnIds)
    {
        if (card.ColumnId is int columnId && doneColumnIds.Contains(columnId))
        {
            return false;
        }

        var deadline = BusinessMapToolContext.ParseDate(card.Deadline);
        return deadline is not null && deadline < now;
    }

    private async Task<Dictionary<int, string?>?> TryGetWorkspaceNamesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var workspaces = await _context.ExecuteAsync(
                () => _context.Client.Workspaces.GetWorkspacesAsync(cancellationToken),
                "list workspaces").ConfigureAwait(false);

            return workspaces.ToDictionary(w => w.WorkspaceId, w => w.Name);
        }
        catch (ToolException ex)
        {
            _logger.LogWarning(ex, "Could not resolve workspace names; board list will omit them.");
            return null;
        }
    }
}
