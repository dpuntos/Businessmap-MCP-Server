namespace BusinessMapNET.Core.Services;

/// <summary>
/// Default implementation of <see cref="IBusinessMapClient"/> that aggregates the resource clients.
/// </summary>
public sealed class BusinessMapClient : IBusinessMapClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessMapClient"/> class.
    /// </summary>
    public BusinessMapClient(
        IUsersApi users,
        IWorkspacesApi workspaces,
        IBoardsApi boards,
        ICardsApi cards)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
        Workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        Boards = boards ?? throw new ArgumentNullException(nameof(boards));
        Cards = cards ?? throw new ArgumentNullException(nameof(cards));
    }

    /// <inheritdoc />
    public IUsersApi Users { get; }

    /// <inheritdoc />
    public IWorkspacesApi Workspaces { get; }

    /// <inheritdoc />
    public IBoardsApi Boards { get; }

    /// <inheritdoc />
    public ICardsApi Cards { get; }
}
