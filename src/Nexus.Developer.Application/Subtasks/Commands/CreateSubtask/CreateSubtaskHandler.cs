using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Subtasks;
using DomainSubtask = Nexus.Developer.Core.Subtasks.Subtask;

namespace Nexus.Developer.Application.Subtasks.Commands.CreateSubtask;

public sealed class CreateSubtaskHandler
{
    private readonly ISubtaskRepository _repository;

    public CreateSubtaskHandler(ISubtaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateSubtaskResult> HandleAsync(
        CreateSubtaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var subtask = new DomainSubtask(
            SubtaskId.New(),
            command.TaskId,
            command.Title,
            command.Description,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(subtask, cancellationToken);

        return new CreateSubtaskResult(
            subtask.Id,
            subtask.Title,
            subtask.Reference);
    }
}
