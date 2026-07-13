using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Encapsulates the Businessmap endpoints related to users.
/// </summary>
public interface IUsersApi
{
    /// <summary>
    /// Gets the details of the currently authenticated user (<c>GET /me</c>).
    /// </summary>
    Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every user in the account (<c>GET /users</c>).
    /// </summary>
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default);
}
