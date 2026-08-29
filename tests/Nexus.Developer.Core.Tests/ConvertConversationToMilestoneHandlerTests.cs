using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToMilestone;
using Nexus.Developer.Application.Milestones.Commands.CreateMilestone;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Core.ChatCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.ObjectChatLinks;
using Nexus.Developer.Core.Scope;
using Nexus.Developer.Core.Subtasks;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;
using ITaskRepository = Nexus.Developer.Core.Tasks.ITaskRepository;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class ConvertConversationToMilestoneHandlerTests
{
    [Fact]
    public async Task Convert_WhenConversationExists_CreatesMilestoneAndLink()
    {
        var conversationId = Guid.NewGuid();
        var subprojectId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var targetDate = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat about the milestone", DateTimeOffset.UtcNow));
        var milestoneRepo = new RecordingMilestoneRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(chatCoreClient, milestoneRepo, linkRepo);

        var command = new ConvertConversationToMilestoneCommand(
            ConversationId: conversationId,
            SubprojectId: subprojectId,
            Name: "Converted Milestone",
            Description: "From chat",
            TargetDate: targetDate,
            CreatedByUserId: createdByUserId,
            MessageRangeStart: null,
            MessageRangeEnd: null);

        var result = await handler.HandleAsync(command);

        var milestone = Assert.Single(milestoneRepo.Milestones);
        var link = Assert.Single(linkRepo.Links);

        // Result shape.
        Assert.Equal(milestone.Id, result.MilestoneId);
        Assert.Equal(milestone.Reference, result.MilestoneReference);
        Assert.Equal(milestone.Name, result.Name);
        Assert.Equal(link.Id, result.ObjectChatLinkId);
        Assert.Equal(conversationId, result.ConversationId);

        // Milestone carried the command's fields; CreatedByUserId doubles as creator.
        Assert.Equal(new SubprojectId(subprojectId), milestone.SubprojectId);
        Assert.Equal("Converted Milestone", milestone.Name);
        Assert.Equal("From chat", milestone.Description);
        Assert.Equal(targetDate, milestone.TargetDate);
        Assert.Equal(createdByUserId, milestone.CreatedByUserId);

        // Link points back at the new Milestone and the originating conversation.
        Assert.Equal(conversationId, link.ConversationId);
        Assert.Equal(ObjectChatLinkTargetType.Milestone, link.TargetType);
        Assert.Equal(milestone.Id.Value, link.TargetId);
        Assert.Equal(createdByUserId, link.LinkedByUserId);
    }

    [Fact]
    public async Task Convert_WhenConversationDoesNotExist_ThrowsAndCreatesNothing()
    {
        var conversationId = Guid.NewGuid();
        var milestoneRepo = new RecordingMilestoneRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(new FakeChatCoreClient(), milestoneRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<ConversationNotFoundException>(() =>
            handler.HandleAsync(
                new ConvertConversationToMilestoneCommand(
                    ConversationId: conversationId,
                    SubprojectId: Guid.NewGuid(),
                    Name: "Should not run",
                    Description: null,
                    TargetDate: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        Assert.Equal(conversationId, ex.ConversationId);
        Assert.Empty(milestoneRepo.Milestones);
        Assert.Empty(linkRepo.Links);
    }

    [Fact]
    public async Task Convert_WhenLinkCreationFailsAfterMilestoneCreated_MilestonePersistsAndExceptionPropagates()
    {
        var conversationId = Guid.NewGuid();
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat", DateTimeOffset.UtcNow));
        var milestoneRepo = new RecordingMilestoneRepository();
        // Link persistence fails after the Milestone above has already been saved.
        var linkRepo = new RecordingObjectChatLinkRepository(throwOnAdd: true);
        var handler = CreateHandler(chatCoreClient, milestoneRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new ConvertConversationToMilestoneCommand(
                    ConversationId: conversationId,
                    SubprojectId: Guid.NewGuid(),
                    Name: "Milestone with no link",
                    Description: null,
                    TargetDate: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        // Known limitation of this slice, asserted as a test: the Milestone exists,
        // unlinked, and the failure surfaced -- no compensating delete.
        Assert.Equal("Simulated chat-link persistence failure.", ex.Message);
        Assert.Single(milestoneRepo.Milestones);
        Assert.Empty(linkRepo.Links);
    }

    private static ConvertConversationToMilestoneHandler CreateHandler(
        IChatCoreClient chatCoreClient,
        IMilestoneRepository milestoneRepository,
        IObjectChatLinkRepository objectChatLinkRepository)
    {
        // CreateMilestoneHandler now validates the Subproject exists; that path is
        // covered by CreateMilestoneHandlerTests, so this convert-flow fake always
        // reports the subproject present.
        var createMilestoneHandler = new CreateMilestoneHandler(
            new AlwaysPresentScopeClient(),
            milestoneRepository);
        var createObjectChatLinkHandler = new CreateObjectChatLinkHandler(
            new NullFeatureRepository(),
            new NullTaskRepository(),
            new NullSubtaskRepository(),
            milestoneRepository,
            new NullIssueRepository(),
            objectChatLinkRepository);

        return new ConvertConversationToMilestoneHandler(
            chatCoreClient,
            createMilestoneHandler,
            createObjectChatLinkHandler);
    }

    private sealed class FakeChatCoreClient : IChatCoreClient
    {
        private readonly IReadOnlyDictionary<Guid, ChatCoreConversation> _conversations;

        public FakeChatCoreClient(params ChatCoreConversation[] conversations)
            => _conversations = conversations.ToDictionary(c => c.ConversationId);

        public Task<ChatCoreConversation?> GetConversationAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _conversations.TryGetValue(conversationId, out var conversation) ? conversation : null);
    }

    private sealed class AlwaysPresentScopeClient : IScopeClient
    {
        public Task<ScopeSubproject?> GetSubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ScopeSubproject?>(
                new ScopeSubproject(
                    subprojectId.Value,
                    Guid.NewGuid(),
                    "Existing Subproject",
                    "SP-0000"));
    }

    private sealed class RecordingMilestoneRepository : IMilestoneRepository
    {
        private readonly List<Milestone> _milestones = new();

        public IReadOnlyList<Milestone> Milestones => _milestones;

        public Task AddAsync(Milestone domain, CancellationToken cancellationToken = default)
        {
            _milestones.Add(domain);
            return Task.CompletedTask;
        }

        public Task<Milestone?> GetAsync(MilestoneId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_milestones.FirstOrDefault(milestone => milestone.Id == id));

        public Task UpdateAsync(Milestone domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Milestone>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Milestone>>(Array.Empty<Milestone>());
    }

    private sealed class RecordingObjectChatLinkRepository : IObjectChatLinkRepository
    {
        private readonly List<ObjectChatLink> _links = new();
        private readonly bool _throwOnAdd;

        public RecordingObjectChatLinkRepository(bool throwOnAdd = false) => _throwOnAdd = throwOnAdd;

        public IReadOnlyList<ObjectChatLink> Links => _links;

        public Task AddAsync(ObjectChatLink link, CancellationToken cancellationToken = default)
        {
            if (_throwOnAdd)
            {
                throw new InvalidOperationException("Simulated chat-link persistence failure.");
            }

            _links.Add(link);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ObjectChatLink>> ListByConversationAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ObjectChatLink>>(Array.Empty<ObjectChatLink>());

        public Task<IReadOnlyList<ObjectChatLink>> ListByTargetAsync(
            ObjectChatLinkTargetType targetType,
            Guid targetId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ObjectChatLink>>(Array.Empty<ObjectChatLink>());
    }

    private sealed class NullFeatureRepository : IFeatureRepository
    {
        public Task AddAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Feature?> GetAsync(FeatureId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Feature?>(null);

        public Task UpdateAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Feature>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Feature>>(Array.Empty<Feature>());
    }

    private sealed class NullTaskRepository : ITaskRepository
    {
        public System.Threading.Tasks.Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<DeveloperTask?> GetAsync(
            TaskId id,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<DeveloperTask?>(null);

        public System.Threading.Tasks.Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<IReadOnlyList<DeveloperTask>> ListByFeatureAsync(
            FeatureId featureId,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<IReadOnlyList<DeveloperTask>>(Array.Empty<DeveloperTask>());
    }

    private sealed class NullSubtaskRepository : ISubtaskRepository
    {
        public Task AddAsync(Subtask domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Subtask?> GetAsync(SubtaskId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Subtask?>(null);

        public Task UpdateAsync(Subtask domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Subtask>> ListByTaskAsync(
            TaskId taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subtask>>(Array.Empty<Subtask>());
    }

    private sealed class NullIssueRepository : IIssueRepository
    {
        public Task AddAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Issue?> GetAsync(IssueId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Issue?>(null);

        public Task UpdateAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
