using BusinessMapNET.Application.Internal;
using BusinessMapNET.Core.Models;
using Microsoft.Extensions.Logging;

namespace BusinessMapNET.Application.Services;

/// <inheritdoc />
public sealed class TaskService : ITaskService
{
    private readonly BusinessMapContext _context;
    private readonly ILogger<TaskService> _logger;

    /// <summary>Initializes a new instance of the <see cref="TaskService"/> class.</summary>
    public TaskService(BusinessMapContext context, ILogger<TaskService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CardSubtask> CreateTaskAsync(
        int cardId,
        string description,
        int? assigneeUserId,
        bool assignToMe,
        string? deadline,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new BusinessMapServiceException("'cardId' must be a positive card id.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new BusinessMapServiceException("'description' is required and cannot be empty.");
        }

        var deadlineValue = NormalizeDeadline(deadline);
        int? ownerId = assigneeUserId;
        if (assignToMe)
        {
            var me = await _context.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
            if (ownerId is not null && ownerId != me)
            {
                throw new BusinessMapServiceException(
                    "Specify either 'assigneeUserId' or 'assignToMe', not both with different users.");
            }

            ownerId = me;
        }

        var request = new CreateSubtaskRequest
        {
            Description = description.Trim(),
            OwnerUserId = ownerId,
            Deadline = deadlineValue,
        };

        _logger.LogInformation("create_task card={CardId}", cardId);

        return await _context.ExecuteAsync(
            () => _context.Client.Cards.AddCardSubtaskAsync(cardId, request, cancellationToken),
            $"create a subtask on card {cardId}").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CardSubtask> SetTaskCompletionAsync(
        int cardId,
        int subtaskId,
        bool completed,
        CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
        {
            throw new BusinessMapServiceException("'cardId' must be a positive card id.");
        }

        if (subtaskId <= 0)
        {
            throw new BusinessMapServiceException("'subtaskId' must be a positive subtask id.");
        }

        var request = new UpdateSubtaskRequest
        {
            IsFinished = completed ? 1 : 0,
        };

        _logger.LogInformation(
            "complete_task card={CardId} subtask={SubtaskId} completed={Completed}",
            cardId,
            subtaskId,
            completed);

        return await _context.ExecuteAsync(
            () => _context.Client.Cards.UpdateCardSubtaskAsync(cardId, subtaskId, request, cancellationToken),
            $"update subtask {subtaskId} of card {cardId}").ConfigureAwait(false);
    }

    private static string? NormalizeDeadline(string? deadline)
    {
        if (string.IsNullOrWhiteSpace(deadline))
        {
            return null;
        }

        return DateParsing.Parse(deadline) is not null
            ? deadline.Trim()
            : throw new BusinessMapServiceException($"'deadline' is not a valid ISO 8601 date/time: '{deadline}'.");
    }
}
