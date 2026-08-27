using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainIssue = Nexus.Developer.Core.Issues.Issue;
using DomainIssueLink = Nexus.Developer.Core.Issues.IssueLink;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class IssueLinkConfiguration : IEntityTypeConfiguration<DomainIssueLink>
{
    public void Configure(EntityTypeBuilder<DomainIssueLink> builder)
    {
        builder.ToTable("IssueLink");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasConversion(StronglyTypedIdConverters.IssueLinkId)
            .ValueGeneratedNever();

        builder.Property(link => link.IssueId)
            .HasConversion(StronglyTypedIdConverters.IssueId)
            .IsRequired();

        builder.HasOne<DomainIssue>()
            .WithMany()
            .HasForeignKey(link => link.IssueId)
            .HasConstraintName("FK_IssueLink_Issue")
            .OnDelete(DeleteBehavior.Cascade);

        // Universal-attachment target: Workspace/Project/Subproject/Chat are
        // foreign; Feature/Milestone/Task/Subtask/DevelopmentRun are Developer's
        // own -- either way this is a tagged Guid, not an FK (see class remarks
        // on IssueLinkTargetType).
        builder.Property(link => link.TargetType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(link => link.TargetId)
            .IsRequired();

        builder.HasIndex(link => new { link.TargetType, link.TargetId })
            .HasDatabaseName("IX_IssueLink_Target");

        builder.Property(link => link.LinkedByUserId)
            .IsRequired();

        builder.Property(link => link.LinkedAt)
            .IsRequired();
    }
}
