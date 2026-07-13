using System.Text.Json.Serialization;

namespace BusinessMapNET.Core.Http;

/// <summary>
/// Pagination metadata returned by Businessmap list endpoints.
/// </summary>
public sealed class Pagination
{
    /// <summary>
    /// The total number of pages in the paginated result set.
    /// </summary>
    [JsonPropertyName("all_pages")]
    public int AllPages { get; init; }

    /// <summary>
    /// The page number of the current set of results.
    /// </summary>
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; init; }

    /// <summary>
    /// The number of results per page.
    /// </summary>
    [JsonPropertyName("results_per_page")]
    public int ResultsPerPage { get; init; }
}

/// <summary>
/// Represents a single page of results returned by a paginated Businessmap list endpoint.
/// </summary>
/// <typeparam name="T">The type of the items in the page.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Pagination metadata describing the current page.
    /// </summary>
    [JsonPropertyName("pagination")]
    public Pagination? Pagination { get; init; }

    /// <summary>
    /// The list of items for the current page.
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<T> Data { get; init; } = [];
}
