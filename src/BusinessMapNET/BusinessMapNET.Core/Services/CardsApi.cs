using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Default implementation of <see cref="ICardsApi"/>.
/// </summary>
public sealed class CardsApi : BusinessMapApiClient, ICardsApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CardsApi"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    public CardsApi(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc />
    public Task<PagedResult<Card>> GetCardsAsync(CardsQuery? query = null, CancellationToken cancellationToken = default)
    {
        var queryString = query?.ToQueryString() ?? string.Empty;
        return GetPagedAsync<Card>("cards" + queryString, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Card> GetCardAsync(int cardId, CancellationToken cancellationToken = default) =>
        GetAsync<Card>(
            FormattableString.Invariant($"cards/{cardId}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<Card> CreateCardAsync(CreateCardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<Card>("cards", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Card> UpdateCardAsync(int cardId, UpdateCardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PatchAsync<Card>(
            FormattableString.Invariant($"cards/{cardId}"),
            request,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteCardAsync(int cardId, CancellationToken cancellationToken = default) =>
        DeleteAsync(
            FormattableString.Invariant($"cards/{cardId}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<CardComment>> GetCardCommentsAsync(int cardId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<CardComment>>(
            FormattableString.Invariant($"cards/{cardId}/comments"),
            cancellationToken);

    /// <inheritdoc />
    public Task<CardComment> AddCardCommentAsync(int cardId, CreateCommentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CardComment>(
            FormattableString.Invariant($"cards/{cardId}/comments"),
            request,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CardSubtask>> GetCardSubtasksAsync(int cardId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<CardSubtask>>(
            FormattableString.Invariant($"cards/{cardId}/subtasks"),
            cancellationToken);

    /// <inheritdoc />
    public Task<CardSubtask> AddCardSubtaskAsync(int cardId, CreateSubtaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CardSubtask>(
            FormattableString.Invariant($"cards/{cardId}/subtasks"),
            request,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<CardSubtask> UpdateCardSubtaskAsync(int cardId, int subtaskId, UpdateSubtaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PatchAsync<CardSubtask>(
            FormattableString.Invariant($"cards/{cardId}/subtasks/{subtaskId}"),
            request,
            cancellationToken);
    }
}
