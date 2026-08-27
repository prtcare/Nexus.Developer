using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainFeature = Nexus.Developer.Core.Features.Feature;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class FeatureConfiguration : IEntityTypeConfiguration<DomainFeature>
{
    public void Configure(EntityTypeBuilder<DomainFeature> builder)
    {
        builder.ToTable("Feature");

        builder.HasKey(feature => feature.Id);

        builder.Property(feature => feature.Id)
            .HasConversion(StronglyTypedIdConverters.FeatureId)
            .ValueGeneratedNever();

        // SubprojectId is a foreign (Product Core) reference held as an opaque
        // Guid-backed struct -- indexed, but never a foreign key, per AGENTS.md's
        // Boundary rules (Developer must not reference a product database).
        builder.Property(feature => feature.SubprojectId)
            .HasConversion(StronglyTypedIdConverters.SubprojectId)
            .IsRequired();

        builder.HasIndex(feature => feature.SubprojectId)
            .HasDatabaseName("IX_Feature_SubprojectId");

        builder.Property(feature => feature.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(feature => feature.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(feature => feature.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(feature => feature.CreatedByUserId)
            .IsRequired();

        builder.Property(feature => feature.CreatedAt)
            .IsRequired();

        builder.Property<int>("Seq")
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        var reference = builder.Property(feature => feature.Reference)
            .HasColumnName("Ref")
            .HasComputedColumnSql(
                "('FEA-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))",
                stored: true)
            .ValueGeneratedOnAddOrUpdate();

        reference.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        reference.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(feature => feature.Reference)
            .IsUnique()
            .HasDatabaseName("UQ_Feature_Ref");
    }
}
