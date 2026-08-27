using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainIssue = Nexus.Developer.Core.Issues.Issue;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class IssueConfiguration : IEntityTypeConfiguration<DomainIssue>
{
    public void Configure(EntityTypeBuilder<DomainIssue> builder)
    {
        builder.ToTable("Issue");

        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.Id)
            .HasConversion(StronglyTypedIdConverters.IssueId)
            .ValueGeneratedNever();

        builder.Property(issue => issue.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(issue => issue.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(issue => issue.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(issue => issue.CreatedByUserId)
            .IsRequired();

        builder.Property(issue => issue.CreatedAt)
            .IsRequired();

        builder.Property<int>("Seq")
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        var reference = builder.Property(issue => issue.Reference)
            .HasColumnName("Ref")
            .HasComputedColumnSql(
                "('ISS-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))",
                stored: true)
            .ValueGeneratedOnAddOrUpdate();

        reference.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        reference.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(issue => issue.Reference)
            .IsUnique()
            .HasDatabaseName("UQ_Issue_Ref");
    }
}
