using System.ComponentModel;
using BusinessMapNET.Core.Models;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to manage card comments.
/// </summary>
[McpServerToolType]
public sealed class CommentTools
{
    private readonly BusinessMapToolContext _context;
    private readonly ILogger<CommentTools> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentTools"/> class.
    /// </summary>
    public CommentTools(BusinessMapToolContext context, ILogger<CommentTools> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds a comment to a card.
    /// </summary>
    /// <remarks>
    /// Example: <c>add_comment(cardId: 12345, text: "Blocked waiting on design review.")</c>.
    /// </remarks>
    [McpServerTool(Name = "add_comment")]
    [Description(
        "Add a comment to a card. Use this to leave notes, status updates or explanations on a card. " +
        "Returns the created comment. To read existing comments use 'get_card_details'.")]
    public async Task<CommentInfo> AddCommentAsync(
        [Description("The id of the card to comment on.")]
        int cardId,
        [Description("The comment text. Required and non-empty.")]
        string text,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new ToolException("'cardId' must be a positive card id.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ToolException("'text' is required and cannot be empty.");
        }

        var request = new CreateCommentRequest { Text = text.Trim() };

        _logger.LogInformation("add_comment card={CardId}", cardId);

        var comment = await _context.ExecuteAsync(
            () => _context.Client.Cards.AddCardCommentAsync(cardId, request, cancellationToken),
            $"add a comment to card {cardId}").ConfigureAwait(false);

        return BusinessMapToolContext.ToInfo(comment);
    }
}
