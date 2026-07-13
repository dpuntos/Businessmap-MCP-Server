using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Encapsulates the Businessmap endpoints related to cards, including comments and subtasks.
/// </summary>
public interface ICardsApi
{
    /// <summary>
    /// Gets a paginated list of cards matching the supplied filters (<c>GET /cards</c>).
    /// </summary>
    /// <param name="query">Optional filters. When <see langword="null"/>, defaults are used.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<PagedResult<Card>> GetCardsAsync(CardsQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the details of a single card (<c>GET /cards/{card_id}</c>).
    /// </summary>
    Task<Card> GetCardAsync(int cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a card (<c>POST /cards</c>).
    /// </summary>
    Task<Card> CreateCardAsync(CreateCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a card (<c>PATCH /cards/{card_id}</c>).
    /// </summary>
    Task<Card> UpdateCardAsync(int cardId, UpdateCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a card (<c>DELETE /cards/{card_id}</c>).
    /// </summary>
    Task DeleteCardAsync(int cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the comments of a card (<c>GET /cards/{card_id}/comments</c>).
    /// </summary>
    Task<IReadOnlyList<CardComment>> GetCardCommentsAsync(int cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a comment to a card (<c>POST /cards/{card_id}/comments</c>).
    /// </summary>
    Task<CardComment> AddCardCommentAsync(int cardId, CreateCommentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the subtasks of a card (<c>GET /cards/{card_id}/subtasks</c>).
    /// </summary>
    Task<IReadOnlyList<CardSubtask>> GetCardSubtasksAsync(int cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a subtask to a card (<c>POST /cards/{card_id}/subtasks</c>).
    /// </summary>
    Task<CardSubtask> AddCardSubtaskAsync(int cardId, CreateSubtaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a subtask of a card (<c>PATCH /cards/{card_id}/subtasks/{subtask_id}</c>).
    /// </summary>
    Task<CardSubtask> UpdateCardSubtaskAsync(int cardId, int subtaskId, UpdateSubtaskRequest request, CancellationToken cancellationToken = default);
}
