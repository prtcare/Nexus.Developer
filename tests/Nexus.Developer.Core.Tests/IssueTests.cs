using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issue;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class IssueTests
{
    [Fact]
    public void Create_HasNoParentId_UniversallyAttachableByDesign()
    {
        // Deliberately no assertion beyond construction succeeding without any
        // Workspace/Project/Subproject/etc id -- Issue itself carries no parent;
        // positioning is entirely IssueLink's job. See class remarks on Issue.cs.
        var issue = new Issue(IssueId.New(), "Bug in scope picker", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(IssueStatus.Open, issue.Status);
    }

    [Theory]
    [InlineData(IssueLinkTargetType.Workspace)]
    [InlineData(IssueLinkTargetType.Project)]
    [InlineData(IssueLinkTargetType.Subproject)]
    [InlineData(IssueLinkTargetType.Feature)]
    [InlineData(IssueLinkTargetType.Milestone)]
    [InlineData(IssueLinkTargetType.Task)]
    [InlineData(IssueLinkTargetType.Subtask)]
    [InlineData(IssueLinkTargetType.Chat)]
    [InlineData(IssueLinkTargetType.DevelopmentRun)]
    public void IssueLink_AttachesToEveryUniversalTargetType(IssueLinkTargetType targetType)
    {
        var link = new IssueLink(
            IssueLinkId.New(), IssueId.New(), targetType, Guid.NewGuid(),
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(targetType, link.TargetType);
    }

    [Fact]
    public void SameIssue_CanLinkToMultipleTargets_Independently()
    {
        var issueId = IssueId.New();

        var linkToFeature = new IssueLink(IssueLinkId.New(), issueId, IssueLinkTargetType.Feature,
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var linkToChat = new IssueLink(IssueLinkId.New(), issueId, IssueLinkTargetType.Chat,
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(issueId, linkToFeature.IssueId);
        Assert.Equal(issueId, linkToChat.IssueId);
        Assert.NotEqual(linkToFeature.Id, linkToChat.Id);
    }
}
