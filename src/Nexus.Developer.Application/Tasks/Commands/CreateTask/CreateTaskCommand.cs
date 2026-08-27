using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    FeatureId FeatureId,
    string Title,
    string Description,
    Guid CreatedByUserId);
