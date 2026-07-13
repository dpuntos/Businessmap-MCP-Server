using BusinessMapNET.Application.Internal;
using BusinessMapNET.Application.Models;
using BusinessMapNET.Core.Models;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Services;

/// <inheritdoc />
public sealed class BoardService : IBoardService
{
    private const int MaxScanCards = 1000;
    private const int FetchSize = 100;
    private const int MaxOwnersReturned = 25;

    private readonly BusinessMapContext _context;
    private readonly ILogger<BoardService> _logger;

    /// <summary>Initializes a new instance of the <see cref="BoardService"/> class.</summary>
    public BoardService(BusinessMapContext context, ILogger<BoardService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Board>> ListBoardsAsync(
        string? nameContains,
        bool includeArchived,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);

        var boards = await _context.GetBoardsAsync(cancellationToken).ConfigureAwait(false);

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
            .ToList();

        _logger.LogInformation("list_boards returned {Count} board(s).", result.Count);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, string?>?> TryGetWorkspaceNamesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspaces = await _context.ExecuteAsync(
                () => _context.Client.Workspaces.GetWorkspacesAsync(cancellationToken),
                "list workspaces").ConfigureAwait(false);

            return workspaces.ToDictionary(w => w.WorkspaceId, w => w.Name);
        }
        catch (BusinessMapServiceException ex)
        {
            _logger.LogWarning(ex, "Could not resolve workspace names; board list will omit them.");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BoardStatusReport> GetBoardStatusAsync(
        int? boardId,
        string? boardName,
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

        return new BoardStatusReport(
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

        var deadline = DateParsing.Parse(card.Deadline);
        return deadline is not null && deadline < now;
    }
}
