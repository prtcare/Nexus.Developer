using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.DevelopmentRuns;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.ObjectChatLinks;
using Nexus.Developer.Core.Subtasks;
using Nexus.Developer.Infrastructure.Sql.Configurations;
using DomainTask = Nexus.Developer.Core.Tasks.Task;

namespace Nexus.Developer.Infrastructure.Sql;

// Persistence for the F-07-10 Developer domain foundation only (Feature, Task,
// Subtask, Milestone, MilestoneLink, Issue, IssueLink, ObjectChatLink,
// DevelopmentRun). Owns its own database ("NexusDeveloper") and schema ("dev") --
// AGENTS.md's boundary rule forbids Nexus.Developer from using a product
// DbContext or database, and this also anticipates the still-deferred M-02-1.5
// layer-schema convention without needing a later rename.
public sealed class NexusDeveloperDbContext : DbContext
{
    public NexusDeveloperDbContext(DbContextOptions<NexusDeveloperDbContext> options)
        : base(options)
    {
    }

    public DbSet<Feature> Features => Set<Feature>();

    public DbSet<DomainTask> Tasks => Set<DomainTask>();

    public DbSet<Subtask> Subtasks => Set<Subtask>();

    public DbSet<Milestone> Milestones => Set<Milestone>();

    public DbSet<MilestoneLink> MilestoneLinks => Set<MilestoneLink>();

    public DbSet<Issue> Issues => Set<Issue>();

    public DbSet<IssueLink> IssueLinks => Set<IssueLink>();

    public DbSet<ObjectChatLink> ObjectChatLinks => Set<ObjectChatLink>();

    public DbSet<DevelopmentRun> DevelopmentRuns => Set<DevelopmentRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dev");

        modelBuilder.ApplyConfiguration(new FeatureConfiguration());
        modelBuilder.ApplyConfiguration(new TaskConfiguration());
        modelBuilder.ApplyConfiguration(new SubtaskConfiguration());
        modelBuilder.ApplyConfiguration(new MilestoneConfiguration());
        modelBuilder.ApplyConfiguration(new MilestoneLinkConfiguration());
        modelBuilder.ApplyConfiguration(new IssueConfiguration());
        modelBuilder.ApplyConfiguration(new IssueLinkConfiguration());
        modelBuilder.ApplyConfiguration(new ObjectChatLinkConfiguration());
        modelBuilder.ApplyConfiguration(new DevelopmentRunConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
