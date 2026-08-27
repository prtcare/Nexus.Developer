using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Infrastructure.Sql.Conventions;

// One ValueConverter<TId, Guid> per strongly-typed id in Nexus.Developer.Core,
// mirroring the Chat Core's own StronglyTypedIdConverters pattern. Every id here
// -- including SubprojectId, which wraps a foreign Product Core reference -- is
// stored as a plain SQL uniqueidentifier; Nexus.Developer never persists a
// product-owned column type (AGENTS.md Boundary rules).
public static class StronglyTypedIdConverters
{
    public static readonly ValueConverter<FeatureId, Guid> FeatureId =
        new(id => id.Value, value => new FeatureId(value));

    public static readonly ValueConverter<TaskId, Guid> TaskId =
        new(id => id.Value, value => new TaskId(value));

    public static readonly ValueConverter<SubtaskId, Guid> SubtaskId =
        new(id => id.Value, value => new SubtaskId(value));

    public static readonly ValueConverter<MilestoneId, Guid> MilestoneId =
        new(id => id.Value, value => new MilestoneId(value));

    public static readonly ValueConverter<MilestoneLinkId, Guid> MilestoneLinkId =
        new(id => id.Value, value => new MilestoneLinkId(value));

    public static readonly ValueConverter<IssueId, Guid> IssueId =
        new(id => id.Value, value => new IssueId(value));

    public static readonly ValueConverter<IssueLinkId, Guid> IssueLinkId =
        new(id => id.Value, value => new IssueLinkId(value));

    public static readonly ValueConverter<ObjectChatLinkId, Guid> ObjectChatLinkId =
        new(id => id.Value, value => new ObjectChatLinkId(value));

    public static readonly ValueConverter<DevelopmentRunId, Guid> DevelopmentRunId =
        new(id => id.Value, value => new DevelopmentRunId(value));

    public static readonly ValueConverter<SubprojectId, Guid> SubprojectId =
        new(id => id.Value, value => new SubprojectId(value));
}
