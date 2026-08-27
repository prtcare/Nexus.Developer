using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Infrastructure.Sql.Conventions;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class MilestoneLinkConfiguration : IEntityTypeConfiguration<MilestoneLink>
{
    public void Configure(EntityTypeBuilder<MilestoneLink> builder)
    {
        builder.ToTable("MilestoneLink");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasConversion(StronglyTypedIdConverters.MilestoneLinkId)
            .ValueGeneratedNever();

        builder.Property(link => link.MilestoneId)
            .HasConversion(StronglyTypedIdConverters.MilestoneId)
            .IsRequired();

        builder.HasOne<Milestone>()
            .WithMany()
            .HasForeignKey(link => link.MilestoneId)
            .HasConstraintName("FK_MilestoneLink_Milestone")
            .OnDelete(DeleteBehavior.Cascade);

        // TargetType/TargetId: polymorphic tagged Guid across Feature/Task/Subtask.
        // No FK -- it must span three different tables, so it is indexed, not
        // constrained, matching IssueLink and ObjectChatLink below.
        builder.Property(link => link.TargetType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(link => link.TargetId)
            .IsRequired();

        builder.HasIndex(link => new { link.TargetType, link.TargetId })
            .HasDatabaseName("IX_MilestoneLink_Target");

        builder.Property(link => link.LinkedByUserId)
            .IsRequired();

        builder.Property(link => link.LinkedAt)
            .IsRequired();
    }
}
