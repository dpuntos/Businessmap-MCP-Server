using System.ComponentModel;
using BusinessMapNET.Application.Services;
using BusinessMapNET.MCPServer.Dtos;
using BusinessMapNET.MCPServer.Services;
using ModelContextProtocol.Server;

namespace BusinessMapNET.MCPServer.Tools;

/// <summary>
/// High-level MCP tools to discover the users of the account and resolve their numeric ids,
/// which are required to filter cards by assignee (owner). Thin adapter over <see cref="IUserService"/>.
/// </summary>
[McpServerToolType]
public sealed class UserTools
{
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserTools"/> class.
    /// </summary>
    public UserTools(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
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
    public Task<IReadOnlyList<UserSummary>> ListUsersAsync(
        [Description("Optional case-insensitive substring matched against the real name, username or email.")]
        string? nameContains = null,
        [Description("Whether to include disabled/deactivated user accounts. Defaults to false.")]
        bool includeDisabled = false,
        [Description("Maximum number of users to return (1-500). Defaults to 200.")]
        int limit = 200,
        CancellationToken cancellationToken = default) =>
        ToolExecutor.RunAsync(async () =>
        {
            var users = await _userService
                .ListUsersAsync(nameContains, includeDisabled, limit, cancellationToken)
                .ConfigureAwait(false);
            return (IReadOnlyList<UserSummary>)users.Select(DtoMapper.ToSummary).ToList();
        });
}
