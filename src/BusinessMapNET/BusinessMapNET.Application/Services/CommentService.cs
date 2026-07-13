using BusinessMapNET.Core.Models;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Services;

/// <inheritdoc />
public sealed class CommentService : ICommentService
{
    private readonly BusinessMapContext _context;
    private readonly ILogger<CommentService> _logger;

    /// <summary>Initializes a new instance of the <see cref="CommentService"/> class.</summary>
    public CommentService(BusinessMapContext context, ILogger<CommentService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CardComment> AddCommentAsync(
        int cardId,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new BusinessMapServiceException("'cardId' must be a positive card id.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new BusinessMapServiceException("'text' is required and cannot be empty.");
        }

        var request = new CreateCommentRequest { Text = text.Trim() };

        _logger.LogInformation("add_comment card={CardId}", cardId);

        return await _context.ExecuteAsync(
            () => _context.Client.Cards.AddCardCommentAsync(cardId, request, cancellationToken),
            $"add a comment to card {cardId}").ConfigureAwait(false);
    }
}
