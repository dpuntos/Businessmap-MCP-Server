using BusinessMapNET.Core.Http;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Core.Services;

/// <summary>
/// Default implementation of <see cref="IUsersApi"/>.
/// </summary>
public sealed class UsersApi : BusinessMapApiClient, IUsersApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsersApi"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    public UsersApi(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc />
    public Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        GetAsync<User>("me", cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<User>>("users", cancellationToken);
}
