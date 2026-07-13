using System.ComponentModel;
using BusinessMapNET.Application.Services;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to manage card comments. Thin adapter over <see cref="ICommentService"/>.
/// </summary>
[McpServerToolType]
public sealed class CommentTools
{
    private readonly ICommentService _commentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentTools"/> class.
    /// </summary>
    public CommentTools(ICommentService commentService)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
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
    public Task<CommentInfo> AddCommentAsync(
        [Description("The id of the card to comment on.")]
        int cardId,
        [Description("The comment text. Required and non-empty.")]
        string text,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var comment = await _commentService.AddCommentAsync(cardId, text, cancellationToken)
                .ConfigureAwait(false);
            return DtoMapper.ToInfo(comment);
        });
}
