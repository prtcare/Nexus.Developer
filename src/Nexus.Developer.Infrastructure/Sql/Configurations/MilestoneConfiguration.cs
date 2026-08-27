using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainMilestone = Nexus.Developer.Core.Milestones.Milestone;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class MilestoneConfiguration : IEntityTypeConfiguration<DomainMilestone>
{
    public void Configure(EntityTypeBuilder<DomainMilestone> builder)
    {
        builder.ToTable("Milestone");

        builder.HasKey(milestone => milestone.Id);

        builder.Property(milestone => milestone.Id)
            .HasConversion(StronglyTypedIdConverters.MilestoneId)
            .ValueGeneratedNever();

        // Foreign (Product Core) reference, same treatment as Feature.SubprojectId.
        builder.Property(milestone => milestone.SubprojectId)
            .HasConversion(StronglyTypedIdConverters.SubprojectId)
            .IsRequired();

        builder.HasIndex(milestone => milestone.SubprojectId)
            .HasDatabaseName("IX_Milestone_SubprojectId");

        builder.Property(milestone => milestone.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(milestone => milestone.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(milestone => milestone.TargetDate);

        builder.Property(milestone => milestone.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(milestone => milestone.CreatedByUserId)
            .IsRequired();

        builder.Property(milestone => milestone.CreatedAt)
            .IsRequired();

        builder.Property<int>("Seq")
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        var reference = builder.Property(milestone => milestone.Reference)
            .HasColumnName("Ref")
            .HasComputedColumnSql(
                "('MIL-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))",
                stored: true)
            .ValueGeneratedOnAddOrUpdate();

        reference.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        reference.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(milestone => milestone.Reference)
            .IsUnique()
            .HasDatabaseName("UQ_Milestone_Ref");
    }
}
