using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.ObjectChatLinks;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class ObjectChatLinkTests
{
    [Fact]
    public void Create_WithoutMessageRange_LinksWholeConversation()
    {
        var link = new ObjectChatLink(
            ObjectChatLinkId.New(), Guid.NewGuid(), messageRangeStart: null, messageRangeEnd: null,
            ObjectChatLinkTargetType.Feature, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Null(link.MessageRangeStart);
        Assert.Null(link.MessageRangeEnd);
    }

    [Fact]
    public void Create_WithMessageRange_RecordsProvenance()
    {
        var start = Guid.NewGuid();
        var end = Guid.NewGuid();

        var link = new ObjectChatLink(
            ObjectChatLinkId.New(), Guid.NewGuid(), start, end,
            ObjectChatLinkTargetType.Issue, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(start, link.MessageRangeStart);
        Assert.Equal(end, link.MessageRangeEnd);
    }

    [Theory]
    [InlineData(ObjectChatLinkTargetType.Feature)]
    [InlineData(ObjectChatLinkTargetType.Task)]
    [InlineData(ObjectChatLinkTargetType.Subtask)]
    [InlineData(ObjectChatLinkTargetType.Milestone)]
    [InlineData(ObjectChatLinkTargetType.Issue)]
    public void Create_AcceptsEveryDevelopmentObjectType(ObjectChatLinkTargetType targetType)
    {
        var link = new ObjectChatLink(
            ObjectChatLinkId.New(), Guid.NewGuid(), null, null,
            targetType, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(targetType, link.TargetType);
    }

    [Fact]
    public void OneConversation_CanProduceMultipleObjects()
    {
        var conversationId = Guid.NewGuid();

        var toFeature = new ObjectChatLink(ObjectChatLinkId.New(), conversationId, null, null,
            ObjectChatLinkTargetType.Feature, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var toIssue = new ObjectChatLink(ObjectChatLinkId.New(), conversationId, null, null,
            ObjectChatLinkTargetType.Issue, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(conversationId, toFeature.ConversationId);
        Assert.Equal(conversationId, toIssue.ConversationId);
    }
}
