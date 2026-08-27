using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Tasks.Queries.GetTask;

public sealed record GetTaskResult(
    TaskId TaskId,
    FeatureId FeatureId,
    string Title,
    string Description,
    DevelopmentItemStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference,
    Guid? MigratedFromWorkItemId);
