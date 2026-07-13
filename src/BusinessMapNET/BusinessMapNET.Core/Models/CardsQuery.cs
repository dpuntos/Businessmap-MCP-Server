using BusinessMapNET.Core.Http;

namespace BusinessMapNET.Core.Models;

/// <summary>
/// The lifecycle state used to filter cards.
/// </summary>
public enum CardState
{
    /// <summary>Active (non-archived, non-discarded) cards.</summary>
    Active,

    /// <summary>Archived cards.</summary>
    Archived,

    /// <summary>Discarded cards.</summary>
    Discarded
}

/// <summary>
/// Filters for querying cards via <c>GET /cards</c>. Only the most commonly used filters are exposed.
/// Any property left <see langword="null"/> is omitted from the request.
/// </summary>
public sealed class CardsQuery
{
    /// <summary>Restrict to specific card identifiers.</summary>
    public IEnumerable<int>? CardIds { get; set; }

    /// <summary>Restrict to cards on specific boards.</summary>
    public IEnumerable<int>? BoardIds { get; set; }

    /// <summary>Restrict to cards in specific workflows.</summary>
    public IEnumerable<int>? WorkflowIds { get; set; }

    /// <summary>Restrict to cards in specific columns.</summary>
    public IEnumerable<int>? ColumnIds { get; set; }

    /// <summary>Restrict to cards in specific lanes.</summary>
    public IEnumerable<int>? LaneIds { get; set; }

    /// <summary>Restrict to cards owned by specific users.</summary>
    public IEnumerable<int>? OwnerUserIds { get; set; }

    /// <summary>Restrict to cards of specific types.</summary>
    public IEnumerable<int>? TypeIds { get; set; }

    /// <summary>Restrict to cards tagged with specific tag identifiers.</summary>
    public IEnumerable<int>? TagIds { get; set; }

    /// <summary>Restrict to cards matching specific priorities.</summary>
    public IEnumerable<int>? Priorities { get; set; }

    /// <summary>Restrict to cards in the given lifecycle state.</summary>
    public CardState? State { get; set; }

    /// <summary>Restrict to cards that are blocked (<see langword="true"/>) or not (<see langword="false"/>).</summary>
    public bool? IsBlocked { get; set; }

    /// <summary>The page number (1-based) to retrieve.</summary>
    public int? Page { get; set; }

    /// <summary>The number of results per page.</summary>
    public int? PerPage { get; set; }

    /// <summary>
    /// Builds the query string for this filter set.
    /// </summary>
    internal string ToQueryString()
    {
        return new QueryStringBuilder()
            .Add("card_ids", CardIds)
            .Add("board_ids", BoardIds)
            .Add("workflow_ids", WorkflowIds)
            .Add("column_ids", ColumnIds)
            .Add("lane_ids", LaneIds)
            .Add("owner_user_ids", OwnerUserIds)
            .Add("type_ids", TypeIds)
            .Add("tag_ids", TagIds)
            .Add("priorities", Priorities)
            .Add("state", State?.ToString().ToLowerInvariant())
            .Add("is_blocked", IsBlocked)
            .Add("page", Page)
            .Add("per_page", PerPage)
            .Build();
    }
}
