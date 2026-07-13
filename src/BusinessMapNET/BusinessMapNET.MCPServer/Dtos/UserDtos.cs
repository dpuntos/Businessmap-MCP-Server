namespace BusinessMapNET.MCPServer.Dtos;

/// <summary>
/// A lightweight projection of a user, with the minimum information needed to
/// identify them and resolve their numeric id (e.g. to filter cards by assignee).
/// </summary>
/// <param name="UserId">The unique identifier of the user (use this as 'assigneeUserId' in find_cards).</param>
/// <param name="RealName">The real (display) name of the user, e.g. "Jordi Salas".</param>
/// <param name="Username">The username of the user.</param>
/// <param name="Email">The email address of the user.</param>
/// <param name="IsEnabled">Whether the user account is enabled.</param>
public sealed record UserSummary(
    int UserId,
    string? RealName,
    string? Username,
    string? Email,
    bool IsEnabled);
