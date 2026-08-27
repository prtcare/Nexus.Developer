using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Developer.Infrastructure.Sql.Conventions;
using DomainObjectChatLink = Nexus.Developer.Core.ObjectChatLinks.ObjectChatLink;

namespace Nexus.Developer.Infrastructure.Sql.Configurations;

public sealed class ObjectChatLinkConfiguration : IEntityTypeConfiguration<DomainObjectChatLink>
{
    public void Configure(EntityTypeBuilder<DomainObjectChatLink> builder)
    {
        builder.ToTable("ObjectChatLink");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasConversion(StronglyTypedIdConverters.ObjectChatLinkId)
            .ValueGeneratedNever();

        // ConversationId and the message-range ids belong to the Chat Core, which
        // Nexus.Developer does not reference as a domain type (AGENTS.md Boundary
        // rules) -- held and indexed as plain Guids.
        builder.Property(link => link.ConversationId)
            .IsRequired();

        builder.HasIndex(link => link.ConversationId)
            .HasDatabaseName("IX_ObjectChatLink_ConversationId");

        builder.Property(link => link.MessageRangeStart);

        builder.Property(link => link.MessageRangeEnd);

        builder.Property(link => link.TargetType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(link => link.TargetId)
            .IsRequired();

        builder.HasIndex(link => new { link.TargetType, link.TargetId })
            .HasDatabaseName("IX_ObjectChatLink_Target");

        builder.Property(link => link.LinkedByUserId)
            .IsRequired();

        builder.Property(link => link.LinkedAt)
            .IsRequired();
    }
}
