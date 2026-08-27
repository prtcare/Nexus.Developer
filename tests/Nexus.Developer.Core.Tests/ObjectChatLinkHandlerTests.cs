using Nexus.Developer.Application.ObjectChatLinks;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByConversation;
using Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByTarget;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.ObjectChatLinks;
using Nexus.Developer.Core.Subtasks;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;
using ITaskRepository = Nexus.Developer.Core.Tasks.ITaskRepository;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class ObjectChatLinkHandlerTests
{
    [Fact]
    public async Task Create_WithExistingFeatureTarget_CreatesLink()
    {
        var featureId = Guid.NewGuid();
        var linkRepo = new InMemoryObjectChatLinkRepository();
        var handler = CreateHandler(feature: new FakeFeatureRepository(), links: linkRepo);

        var result = await handler.HandleAsync(
            new CreateObjectChatLinkCommand(
                ConversationId: Guid.NewGuid(),
                MessageRangeStart: null,
                MessageRangeEnd: null,
                TargetType: ObjectChatLinkTargetType.Feature,
                TargetId: featureId,
                LinkedByUserId: Guid.NewGuid()));

        Assert.NotEqual(Guid.Empty, result.ObjectChatLinkId.Value);
        Assert.Equal(ObjectChatLinkTargetType.Feature, result.TargetType);
        Assert.Equal(featureId, result.TargetId);
        Assert.Single(linkRepo.Links);
        Assert.Equal(result.ObjectChatLinkId, linkRepo.Links[0].Id);
    }

    [Theory]
    [InlineData(ObjectChatLinkTargetType.Feature)]
    [InlineData(ObjectChatLinkTargetType.Task)]
    [InlineData(ObjectChatLinkTargetType.Subtask)]
    [InlineData(ObjectChatLinkTargetType.Milestone)]
    [InlineData(ObjectChatLinkTargetType.Issue)]
    public async Task Create_WithExistingTargetOfAnyType_CreatesLink(ObjectChatLinkTargetType targetType)
    {
        var linkRepo = new InMemoryObjectChatLinkRepository();
        var handler = CreateHandler(
            feature: targetType == ObjectChatLinkTargetType.Feature ? new FakeFeatureRepository() : null,
            task: targetType == ObjectChatLinkTargetType.Task ? new FakeTaskRepository() : null,
            subtask: targetType == ObjectChatLinkTargetType.Subtask ? new FakeSubtaskRepository() : null,
            milestone: targetType == ObjectChatLinkTargetType.Milestone ? new FakeMilestoneRepository() : null,
            issue: targetType == ObjectChatLinkTargetType.Issue ? new FakeIssueRepository() : null,
            links: linkRepo);

        var result = await handler.HandleAsync(
            new CreateObjectChatLinkCommand(
                ConversationId: Guid.NewGuid(),
                MessageRangeStart: null,
                MessageRangeEnd: null,
                TargetType: targetType,
                TargetId: Guid.NewGuid(),
                LinkedByUserId: Guid.NewGuid()));

        Assert.Equal(targetType, result.TargetType);
        Assert.Single(linkRepo.Links);
        Assert.Equal(targetType, linkRepo.Links[0].TargetType);
    }

    [Fact]
    public async Task Create_WhenTargetDoesNotExist_ThrowsAndPersistsNothing()
    {
        var linkRepo = new InMemoryObjectChatLinkRepository();
        var handler = CreateHandler(feature: new FakeFeatureRepository(exists: false), links: linkRepo);
        var targetId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<ObjectChatLinkTargetNotFoundException>(() =>
            handler.HandleAsync(
                new CreateObjectChatLinkCommand(
                    ConversationId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null,
                    TargetType: ObjectChatLinkTargetType.Feature,
                    TargetId: targetId,
                    LinkedByUserId: Guid.NewGuid())));

        Assert.Equal(ObjectChatLinkTargetType.Feature, ex.TargetType);
        Assert.Equal(targetId, ex.TargetId);
        Assert.Empty(linkRepo.Links);
    }

    [Theory]
    [InlineData(ObjectChatLinkTargetType.Feature)]
    [InlineData(ObjectChatLinkTargetType.Task)]
    [InlineData(ObjectChatLinkTargetType.Subtask)]
    [InlineData(ObjectChatLinkTargetType.Milestone)]
    [InlineData(ObjectChatLinkTargetType.Issue)]
    public async Task Create_WhenTargetOfAnyTypeDoesNotExist_Throws(ObjectChatLinkTargetType targetType)
    {
        var handler = CreateHandler(
            feature: targetType == ObjectChatLinkTargetType.Feature ? new FakeFeatureRepository(exists: false) : null,
            task: targetType == ObjectChatLinkTargetType.Task ? new FakeTaskRepository(exists: false) : null,
            subtask: targetType == ObjectChatLinkTargetType.Subtask ? new FakeSubtaskRepository(exists: false) : null,
            milestone: targetType == ObjectChatLinkTargetType.Milestone ? new FakeMilestoneRepository(exists: false) : null,
            issue: targetType == ObjectChatLinkTargetType.Issue ? new FakeIssueRepository(exists: false) : null);

        var ex = await Assert.ThrowsAsync<ObjectChatLinkTargetNotFoundException>(() =>
            handler.HandleAsync(
                new CreateObjectChatLinkCommand(
                    ConversationId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null,
                    TargetType: targetType,
                    TargetId: Guid.NewGuid(),
                    LinkedByUserId: Guid.NewGuid())));

        Assert.Equal(targetType, ex.TargetType);
    }

    [Fact]
    public async Task ListByConversation_ReturnsOnlyThatConversationsLinks()
    {
        var conversationId = Guid.NewGuid();
        var otherConversationId = Guid.NewGuid();
        var linkRepo = new InMemoryObjectChatLinkRepository();

        await linkRepo.AddAsync(NewLink(conversationId, ObjectChatLinkTargetType.Feature));
        await linkRepo.AddAsync(NewLink(conversationId, ObjectChatLinkTargetType.Issue));
        await linkRepo.AddAsync(NewLink(otherConversationId, ObjectChatLinkTargetType.Task));

        var handler = new ListObjectChatLinksByConversationHandler(linkRepo);

        var result = await handler.HandleAsync(
            new ListObjectChatLinksByConversationQuery(conversationId));

        Assert.Equal(2, result.Links.Count);
        Assert.All(result.Links, link => Assert.Equal(conversationId, link.ConversationId));
    }

    [Fact]
    public async Task ListByTarget_ReturnsEveryConversationLinkedToThatObject()
    {
        var targetType = ObjectChatLinkTargetType.Issue;
        var targetId = Guid.NewGuid();
        var linkRepo = new InMemoryObjectChatLinkRepository();

        await linkRepo.AddAsync(NewLink(conversationId: Guid.NewGuid(), targetType: targetType, targetId: targetId));
        await linkRepo.AddAsync(NewLink(conversationId: Guid.NewGuid(), targetType: targetType, targetId: targetId));
        await linkRepo.AddAsync(NewLink(conversationId: Guid.NewGuid(), targetType: ObjectChatLinkTargetType.Feature));

        var handler = new ListObjectChatLinksByTargetHandler(linkRepo);

        var result = await handler.HandleAsync(
            new ListObjectChatLinksByTargetQuery(targetType, targetId));

        Assert.Equal(2, result.Links.Count);
        Assert.All(result.Links, link =>
        {
            Assert.Equal(targetType, link.TargetType);
            Assert.Equal(targetId, link.TargetId);
        });
    }

    private static CreateObjectChatLinkHandler CreateHandler(
        IFeatureRepository? feature = null,
        ITaskRepository? task = null,
        ISubtaskRepository? subtask = null,
        IMilestoneRepository? milestone = null,
        IIssueRepository? issue = null,
        IObjectChatLinkRepository? links = null)
        => new(
            feature ?? new FakeFeatureRepository(),
            task ?? new FakeTaskRepository(),
            subtask ?? new FakeSubtaskRepository(),
            milestone ?? new FakeMilestoneRepository(),
            issue ?? new FakeIssueRepository(),
            links ?? new InMemoryObjectChatLinkRepository());

    private static ObjectChatLink NewLink(
        Guid conversationId,
        ObjectChatLinkTargetType targetType,
        Guid? targetId = null)
        => new(
            ObjectChatLinkId.New(),
            conversationId,
            messageRangeStart: null,
            messageRangeEnd: null,
            targetType,
            targetId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

    private sealed class InMemoryObjectChatLinkRepository : IObjectChatLinkRepository
    {
        private readonly List<ObjectChatLink> _links = new();

        public IReadOnlyList<ObjectChatLink> Links => _links;

        public Task AddAsync(ObjectChatLink link, CancellationToken cancellationToken = default)
        {
            _links.Add(link);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ObjectChatLink>> ListByConversationAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ObjectChatLink>>(
                _links.Where(link => link.ConversationId == conversationId).ToList());

        public Task<IReadOnlyList<ObjectChatLink>> ListByTargetAsync(
            ObjectChatLinkTargetType targetType,
            Guid targetId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ObjectChatLink>>(
                _links.Where(link => link.TargetType == targetType && link.TargetId == targetId).ToList());
    }

    private sealed class FakeFeatureRepository : IFeatureRepository
    {
        private readonly bool _exists;

        public FakeFeatureRepository(bool exists = true) => _exists = exists;

        public Task AddAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Feature?> GetAsync(FeatureId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Feature?>(_exists
                ? new Feature(id, SubprojectId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public Task UpdateAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Feature>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Feature>>(Array.Empty<Feature>());
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly bool _exists;

        public FakeTaskRepository(bool exists = true) => _exists = exists;

        public System.Threading.Tasks.Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<DeveloperTask?> GetAsync(
            TaskId id,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<DeveloperTask?>(_exists
                ? new DeveloperTask(id, FeatureId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public System.Threading.Tasks.Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<IReadOnlyList<DeveloperTask>> ListByFeatureAsync(
            FeatureId featureId,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<IReadOnlyList<DeveloperTask>>(Array.Empty<DeveloperTask>());
    }

    private sealed class FakeSubtaskRepository : ISubtaskRepository
    {
        private readonly bool _exists;

        public FakeSubtaskRepository(bool exists = true) => _exists = exists;

        public Task AddAsync(Subtask domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Subtask?> GetAsync(SubtaskId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Subtask?>(_exists
                ? new Subtask(id, TaskId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public Task UpdateAsync(Subtask domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Subtask>> ListByTaskAsync(
            TaskId taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subtask>>(Array.Empty<Subtask>());
    }

    private sealed class FakeMilestoneRepository : IMilestoneRepository
    {
        private readonly bool _exists;

        public FakeMilestoneRepository(bool exists = true) => _exists = exists;

        public Task AddAsync(Milestone domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Milestone?> GetAsync(MilestoneId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Milestone?>(_exists
                ? new Milestone(id, SubprojectId.New(), "Name", "Description", null, Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public Task UpdateAsync(Milestone domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Milestone>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Milestone>>(Array.Empty<Milestone>());
    }

    private sealed class FakeIssueRepository : IIssueRepository
    {
        private readonly bool _exists;

        public FakeIssueRepository(bool exists = true) => _exists = exists;

        public Task AddAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Issue?> GetAsync(IssueId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Issue?>(_exists
                ? new Issue(id, "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public Task UpdateAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
