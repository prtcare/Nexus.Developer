using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainTask = Nexus.Developer.Core.Tasks.Task;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class TaskConfiguration : IEntityTypeConfiguration<DomainTask>
{
    public void Configure(EntityTypeBuilder<DomainTask> builder)
    {
        builder.ToTable("Task");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .HasConversion(StronglyTypedIdConverters.TaskId)
            .ValueGeneratedNever();

        builder.Property(task => task.FeatureId)
            .HasConversion(StronglyTypedIdConverters.FeatureId)
            .IsRequired();

        // Real FK: Task is owned by Feature within Nexus.Developer's own hierarchy
        // (Feature > Task > Subtask) -- not a cross-boundary reference.
        builder.HasOne<Feature>()
            .WithMany()
            .HasForeignKey(task => task.FeatureId)
            .HasConstraintName("FK_Task_Feature")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(task => task.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(task => task.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(task => task.CreatedByUserId)
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .IsRequired();

        // WI-07-10.2.1 migration provenance -- non-unique (a WorkItem maps to
        // exactly one Task by convention, but nothing here enforces that at the
        // database level; the migration script itself is the single writer).
        builder.Property(task => task.MigratedFromWorkItemId);

        builder.HasIndex(task => task.MigratedFromWorkItemId)
            .HasDatabaseName("IX_Task_MigratedFromWorkItemId");

        builder.Property<int>("Seq")
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        var reference = builder.Property(task => task.Reference)
            .HasColumnName("Ref")
            .HasComputedColumnSql(
                "('TSK-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))",
                stored: true)
            .ValueGeneratedOnAddOrUpdate();

        reference.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        reference.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(task => task.Reference)
            .IsUnique()
            .HasDatabaseName("UQ_Task_Ref");
    }
}
