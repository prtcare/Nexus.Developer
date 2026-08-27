using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainDevelopmentRun = Nexus.Developer.Core.DevelopmentRuns.DevelopmentRun;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class DevelopmentRunConfiguration : IEntityTypeConfiguration<DomainDevelopmentRun>
{
    public void Configure(EntityTypeBuilder<DomainDevelopmentRun> builder)
    {
        builder.ToTable("DevelopmentRun");

        builder.HasKey(run => run.Id);

        builder.Property(run => run.Id)
            .HasConversion(StronglyTypedIdConverters.DevelopmentRunId)
            .ValueGeneratedNever();

        builder.Property(run => run.TargetType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(run => run.TargetId)
            .IsRequired();

        builder.HasIndex(run => new { run.TargetType, run.TargetId })
            .HasDatabaseName("IX_DevelopmentRun_Target");

        builder.Property(run => run.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(run => run.CreatedByUserId)
            .IsRequired();

        builder.Property(run => run.CreatedAt)
            .IsRequired();

        // Phase 2 placeholders (WI-07-10.3.1) -- reserved nullable columns, never
        // populated or read by any Phase 1 code path. See class remarks on
        // DevelopmentRun for why these exist now instead of a later migration.
        builder.Property(run => run.PlanId);
        builder.Property(run => run.PromptId);
        builder.Property(run => run.ResultId);
        builder.Property(run => run.ReportId);
        builder.Property(run => run.CheckSetId);
        builder.Property(run => run.VerificationId);

        builder.Property<int>("Seq")
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        // RUN-###### (6 digits) per the user's own spec text, unlike the 8-digit
        // Ref pattern used by every other Phase 1 aggregate.
        var reference = builder.Property(run => run.Reference)
            .HasColumnName("Ref")
            .HasComputedColumnSql(
                "('RUN-' + RIGHT('000000' + CAST([Seq] AS varchar(6)), 6))",
                stored: true)
            .ValueGeneratedOnAddOrUpdate();

        reference.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        reference.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(run => run.Reference)
            .IsUnique()
            .HasDatabaseName("UQ_DevelopmentRun_Ref");
    }
}
