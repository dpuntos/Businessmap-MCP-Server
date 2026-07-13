using BusinessMapNET.Application.Models;
using BusinessMapNET.Core.Models;

namespace BusinessMapNET.Application.Services;

/// <summary>Business operations to manage the checklist (subtasks) of a card.</summary>
public interface ITaskService
{
    /// <summary>Creates a subtask (checklist item) inside a card.</summary>
    Task<CardSubtask> CreateTaskAsync(
        int cardId,
        string description,
        int? assigneeUserId,
        bool assignToMe,
        string? deadline,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a subtask as completed or reopens it.</summary>
    Task<CardSubtask> SetTaskCompletionAsync(
        int cardId,
        int subtaskId,
        bool completed,
        CancellationToken cancellationToken = default);
}

/// <summary>Business operations to manage card comments.</summary>
public interface ICommentService
{
    /// <summary>Adds a comment to a card.</summary>
    Task<CardComment> AddCommentAsync(int cardId, string text, CancellationToken cancellationToken = default);
}

/// <summary>Business operations to discover users.</summary>
public interface IUserService
{
    /// <summary>Lists the account users, optionally filtered by a name/username/email substring.</summary>
    Task<IReadOnlyList<User>> ListUsersAsync(
        string? nameContains,
        bool includeDisabled,
        int limit,
        CancellationToken cancellationToken = default);
}

/// <summary>Business operations to inspect the structure of a board.</summary>
public interface IWorkflowService
{
    /// <summary>Returns the parsed structure (workflows, columns, lanes, card types) of a board.</summary>
    Task<BoardStructure> GetWorkflowAsync(
        int? boardId,
        string? boardName,
        CancellationToken cancellationToken = default);
}
