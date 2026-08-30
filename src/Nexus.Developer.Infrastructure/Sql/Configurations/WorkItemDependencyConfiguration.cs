using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainWorkItemDependency = Nexus.Developer.Core.Dependencies.WorkItemDependency;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class WorkItemDependencyConfiguration : IEntityTypeConfiguration<DomainWorkItemDependency>
{
    public void Configure(EntityTypeBuilder<DomainWorkItemDependency> builder)
    {
        builder.ToTable("WorkItemDependency");

        builder.HasKey(dependency => dependency.Id);

        builder.Property(dependency => dependency.Id)
            .HasConversion(StronglyTypedIdConverters.WorkItemDependencyId)
            .ValueGeneratedNever();

        builder.Property(dependency => dependency.UpstreamType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(dependency => dependency.UpstreamId)
            .IsRequired();

        // One index per list-by-node direction.
        builder.HasIndex(dependency => new { dependency.UpstreamType, dependency.UpstreamId })
            .HasDatabaseName("IX_WorkItemDependency_Upstream");

        builder.Property(dependency => dependency.DownstreamType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(dependency => dependency.DownstreamId)
            .IsRequired();

        builder.HasIndex(dependency => new { dependency.DownstreamType, dependency.DownstreamId })
            .HasDatabaseName("IX_WorkItemDependency_Downstream");

        builder.Property(dependency => dependency.Kind)
            .HasConversion<int>()
            .IsRequired();

        // RequiredState is nullable on the edge: null means the upstream must reach
        // full completion before downstream may proceed (WI-07-2.1.1).
        builder.Property(dependency => dependency.RequiredState)
            .HasConversion<int>();

        builder.Property(dependency => dependency.CreatedByUserId)
            .IsRequired();

        builder.Property(dependency => dependency.CreatedAt)
            .IsRequired();
    }
}
