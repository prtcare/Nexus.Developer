using Nexus.Developer.Application.Tasks.Queries.GetTask;

namespace Nexus.Developer.Application.Tasks.Queries.ListTasksByFeature;

public sealed record ListTasksByFeatureResult(
    IReadOnlyList<GetTaskResult> Tasks);
