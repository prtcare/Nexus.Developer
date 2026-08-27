using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskResult(
    TaskId TaskId,
    string Title,
    string Reference);
