using BusinessMapNET.Core.Models;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Services;

/// <inheritdoc />
public sealed class UserService : IUserService
{
    private readonly BusinessMapContext _context;
    private readonly ILogger<UserService> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserService"/> class.</summary>
    public UserService(BusinessMapContext context, ILogger<UserService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> ListUsersAsync(
        string? nameContains,
        bool includeDisabled,
        int limit,
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
            .ToList();

        _logger.LogInformation("list_users returned {Count} user(s).", result.Count);
        return result;
    }

    private static bool Contains(string? value, string term) =>
        value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
