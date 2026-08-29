using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToFeature;
using Nexus.Developer.Application.Features.Commands.CreateFeature;
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

public class ConvertConversationToFeatureHandlerTests
{
    [Fact]
    public async Task Convert_WhenConversationExists_CreatesFeatureAndLink()
    {
        var conversationId = Guid.NewGuid();
        var subprojectId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat about the feature", DateTimeOffset.UtcNow));
        var featureRepo = new RecordingFeatureRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(chatCoreClient, featureRepo, linkRepo);

        var command = new ConvertConversationToFeatureCommand(
            ConversationId: conversationId,
            SubprojectId: subprojectId,
            Title: "Converted Feature",
            Description: "From chat",
            CreatedByUserId: createdByUserId,
            MessageRangeStart: null,
            MessageRangeEnd: null);

        var result = await handler.HandleAsync(command);

        var feature = Assert.Single(featureRepo.Features);
        var link = Assert.Single(linkRepo.Links);

        // Result shape.
        Assert.Equal(feature.Id, result.FeatureId);
        Assert.Equal(feature.Reference, result.FeatureReference);
        Assert.Equal(feature.Title, result.Title);
        Assert.Equal(link.Id, result.ObjectChatLinkId);
        Assert.Equal(conversationId, result.ConversationId);

        // Feature carried the command's fields; CreatedByUserId doubles as creator.
        Assert.Equal(new SubprojectId(subprojectId), feature.SubprojectId);
        Assert.Equal("Converted Feature", feature.Title);
        Assert.Equal("From chat", feature.Description);
        Assert.Equal(createdByUserId, feature.CreatedByUserId);

        // Link points back at the new Feature and the originating conversation.
        Assert.Equal(conversationId, link.ConversationId);
        Assert.Equal(ObjectChatLinkTargetType.Feature, link.TargetType);
        Assert.Equal(feature.Id.Value, link.TargetId);
        Assert.Equal(createdByUserId, link.LinkedByUserId);
    }

    [Fact]
    public async Task Convert_WhenConversationDoesNotExist_ThrowsAndCreatesNothing()
    {
        var conversationId = Guid.NewGuid();
        var featureRepo = new RecordingFeatureRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(new FakeChatCoreClient(), featureRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<ConversationNotFoundException>(() =>
            handler.HandleAsync(
                new ConvertConversationToFeatureCommand(
                    ConversationId: conversationId,
                    SubprojectId: Guid.NewGuid(),
                    Title: "Should not run",
                    Description: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        Assert.Equal(conversationId, ex.ConversationId);
        Assert.Empty(featureRepo.Features);
        Assert.Empty(linkRepo.Links);
    }

    [Fact]
    public async Task Convert_WhenLinkCreationFailsAfterFeatureCreated_FeaturePersistsAndExceptionPropagates()
    {
        var conversationId = Guid.NewGuid();
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat", DateTimeOffset.UtcNow));
        var featureRepo = new RecordingFeatureRepository();
        // Link persistence fails after the Feature above has already been saved.
        var linkRepo = new RecordingObjectChatLinkRepository(throwOnAdd: true);
        var handler = CreateHandler(chatCoreClient, featureRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new ConvertConversationToFeatureCommand(
                    ConversationId: conversationId,
                    SubprojectId: Guid.NewGuid(),
                    Title: "Feature with no link",
                    Description: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        // Known limitation of this slice, asserted as a test: the Feature exists,
        // unlinked, and the failure surfaced -- no compensating delete.
        Assert.Equal("Simulated chat-link persistence failure.", ex.Message);
        Assert.Single(featureRepo.Features);
        Assert.Empty(linkRepo.Links);
    }

    private static ConvertConversationToFeatureHandler CreateHandler(
        IChatCoreClient chatCoreClient,
        IFeatureRepository featureRepository,
        IObjectChatLinkRepository objectChatLinkRepository)
    {
        // CreateFeatureHandler now validates the Subproject exists; that path is
        // covered by CreateFeatureHandlerTests, so this convert-flow fake always
        // reports the subproject present.
        var createFeatureHandler = new CreateFeatureHandler(
            new AlwaysPresentScopeClient(),
            featureRepository);
        var createObjectChatLinkHandler = new CreateObjectChatLinkHandler(
            featureRepository,
            new NullTaskRepository(),
            new NullSubtaskRepository(),
            new NullMilestoneRepository(),
            new NullIssueRepository(),
            objectChatLinkRepository);

        return new ConvertConversationToFeatureHandler(
            chatCoreClient,
            createFeatureHandler,
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

    private sealed class RecordingFeatureRepository : IFeatureRepository
    {
        private readonly List<Feature> _features = new();

        public IReadOnlyList<Feature> Features => _features;

        public Task AddAsync(Feature domain, CancellationToken cancellationToken = default)
        {
            _features.Add(domain);
            return Task.CompletedTask;
        }

        public Task<Feature?> GetAsync(FeatureId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_features.FirstOrDefault(feature => feature.Id == id));

        public Task UpdateAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Feature>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Feature>>(Array.Empty<Feature>());
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

    private sealed class NullMilestoneRepository : IMilestoneRepository
    {
        public Task AddAsync(Milestone domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Milestone?> GetAsync(MilestoneId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Milestone?>(null);

        public Task UpdateAsync(Milestone domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Milestone>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Milestone>>(Array.Empty<Milestone>());
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
