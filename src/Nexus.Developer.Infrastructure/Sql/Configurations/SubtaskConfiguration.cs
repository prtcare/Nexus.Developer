using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainSubtask = Nexus.Developer.Core.Subtasks.Subtask;
using DomainTask = Nexus.Developer.Core.Tasks.Task;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class SubtaskConfiguration : IEntityTypeConfiguration<DomainSubtask>
{
    public void Configure(EntityTypeBuilder<DomainSubtask> builder)
    {
        builder.ToTable("Subtask");

        builder.HasKey(subtask => subtask.Id);

        builder.Property(subtask => subtask.Id)
            .HasConversion(StronglyTypedIdConverters.SubtaskId)
            .ValueGeneratedNever();

        builder.Property(subtask => subtask.TaskId)
            .HasConversion(StronglyTypedIdConverters.TaskId)
            .IsRequired();

        builder.HasOne<DomainTask>()
            .WithMany()
            .HasForeignKey(subtask => subtask.TaskId)
            .HasConstraintName("FK_Subtask_Task")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(subtask => subtask.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(subtask => subtask.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(subtask => subtask.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(subtask => subtask.CreatedByUserId)
            .IsRequired();

        builder.Property(subtask => subtask.CreatedAt)
            .IsRequired();

        builder.Property<int>("Seq")
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        var reference = builder.Property(subtask => subtask.Reference)
            .HasColumnName("Ref")
            .HasComputedColumnSql(
                "('SUB-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))",
                stored: true)
            .ValueGeneratedOnAddOrUpdate();

        reference.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        reference.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(subtask => subtask.Reference)
            .IsUnique()
            .HasDatabaseName("UQ_Subtask_Ref");
    }
}
