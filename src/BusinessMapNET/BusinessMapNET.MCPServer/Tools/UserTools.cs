using System.ComponentModel;
using BusinessMapNET.Core.Models;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to discover the users of the account and resolve their numeric ids,
/// which are required to filter cards by assignee (owner).
/// </summary>
[McpServerToolType]
public sealed class UserTools
{
    private readonly BusinessMapToolContext _context;
    private readonly ILogger<UserTools> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserTools"/> class.
    /// </summary>
    public UserTools(BusinessMapToolContext context, ILogger<UserTools> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists the account users, optionally filtered by a name/username/email substring.
    /// </summary>
    /// <remarks>
    /// Example: resolve the id of "Jordi Salas" so their cards can be found:
    /// <code>
    /// list_users(nameContains: "Jordi Salas")
    /// </code>
    /// </remarks>
    [McpServerTool(Name = "list_users")]
    [Description(
        "List the users of the Kanbanize (Businessmap) account, including their numeric user id, real name, " +
        "username and email. Use this to resolve a person's name (e.g. 'Jordi Salas') into the 'assigneeUserId' " +
        "required by 'find_cards', or to translate the owner ids returned by 'get_board_status' into names. " +
        "Supports an optional case-insensitive filter matched against real name, username and email.")]
    public async Task<IReadOnlyList<UserSummary>> ListUsersAsync(
        [Description("Optional case-insensitive substring matched against the real name, username or email.")]
        string? nameContains = null,
        [Description("Whether to include disabled/deactivated user accounts. Defaults to false.")]
        bool includeDisabled = false,
        [Description("Maximum number of users to return (1-500). Defaults to 200.")]
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var users = await _context.GetUsersAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<User> filtered = users;

        if (!includeDisabled)
        {
            filtered = filtered.Where(u => u.IsEnabled == 1);
        }

        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            var term = nameContains.Trim();
            filtered = filtered.Where(u =>
                Contains(u.RealName, term) ||
                Contains(u.Username, term) ||
                Contains(u.Email, term));
        }

        var result = filtered
            .OrderBy(u => u.RealName ?? u.Username, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(BusinessMapToolContext.ToSummary)
            .ToList();

        _logger.LogInformation("list_users returned {Count} user(s).", result.Count);
        return result;
    }

    private static bool Contains(string? value, string term) =>
        value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
