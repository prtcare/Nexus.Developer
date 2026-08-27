using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToSubtask;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Application.Subtasks.Commands.CreateSubtask;
using Nexus.Developer.Core.ChatCore;
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

public class ConvertConversationToSubtaskHandlerTests
{
    [Fact]
    public async Task Convert_WhenConversationExists_CreatesSubtaskAndLink()
    {
        var conversationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat about the subtask", DateTimeOffset.UtcNow));
        var subtaskRepo = new RecordingSubtaskRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(chatCoreClient, subtaskRepo, linkRepo);

        var command = new ConvertConversationToSubtaskCommand(
            ConversationId: conversationId,
            TaskId: taskId,
            Title: "Converted Subtask",
            Description: "From chat",
            CreatedByUserId: createdByUserId,
            MessageRangeStart: null,
            MessageRangeEnd: null);

        var result = await handler.HandleAsync(command);

        var subtask = Assert.Single(subtaskRepo.Subtasks);
        var link = Assert.Single(linkRepo.Links);

        // Result shape.
        Assert.Equal(subtask.Id, result.SubtaskId);
        Assert.Equal(subtask.Reference, result.SubtaskReference);
        Assert.Equal(subtask.Title, result.Title);
        Assert.Equal(link.Id, result.ObjectChatLinkId);
        Assert.Equal(conversationId, result.ConversationId);

        // Subtask carried the command's fields; CreatedByUserId doubles as creator.
        Assert.Equal(new TaskId(taskId), subtask.TaskId);
        Assert.Equal("Converted Subtask", subtask.Title);
        Assert.Equal("From chat", subtask.Description);
        Assert.Equal(createdByUserId, subtask.CreatedByUserId);

        // Link points back at the new Subtask and the originating conversation.
        Assert.Equal(conversationId, link.ConversationId);
        Assert.Equal(ObjectChatLinkTargetType.Subtask, link.TargetType);
        Assert.Equal(subtask.Id.Value, link.TargetId);
        Assert.Equal(createdByUserId, link.LinkedByUserId);
    }

    [Fact]
    public async Task Convert_WhenConversationDoesNotExist_ThrowsAndCreatesNothing()
    {
        var conversationId = Guid.NewGuid();
        var subtaskRepo = new RecordingSubtaskRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(new FakeChatCoreClient(), subtaskRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<ConversationNotFoundException>(() =>
            handler.HandleAsync(
                new ConvertConversationToSubtaskCommand(
                    ConversationId: conversationId,
                    TaskId: Guid.NewGuid(),
                    Title: "Should not run",
                    Description: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        Assert.Equal(conversationId, ex.ConversationId);
        Assert.Empty(subtaskRepo.Subtasks);
        Assert.Empty(linkRepo.Links);
    }

    [Fact]
    public async Task Convert_WhenLinkCreationFailsAfterSubtaskCreated_SubtaskPersistsAndExceptionPropagates()
    {
        var conversationId = Guid.NewGuid();
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat", DateTimeOffset.UtcNow));
        var subtaskRepo = new RecordingSubtaskRepository();
        // Link persistence fails after the Subtask above has already been saved.
        var linkRepo = new RecordingObjectChatLinkRepository(throwOnAdd: true);
        var handler = CreateHandler(chatCoreClient, subtaskRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new ConvertConversationToSubtaskCommand(
                    ConversationId: conversationId,
                    TaskId: Guid.NewGuid(),
                    Title: "Subtask with no link",
                    Description: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        // Known limitation of this slice, asserted as a test: the Subtask exists,
        // unlinked, and the failure surfaced -- no compensating delete.
        Assert.Equal("Simulated chat-link persistence failure.", ex.Message);
        Assert.Single(subtaskRepo.Subtasks);
        Assert.Empty(linkRepo.Links);
    }

    private static ConvertConversationToSubtaskHandler CreateHandler(
        IChatCoreClient chatCoreClient,
        ISubtaskRepository subtaskRepository,
        IObjectChatLinkRepository objectChatLinkRepository)
    {
        var createSubtaskHandler = new CreateSubtaskHandler(subtaskRepository);
        var createObjectChatLinkHandler = new CreateObjectChatLinkHandler(
            new NullFeatureRepository(),
            new NullTaskRepository(),
            subtaskRepository,
            new NullMilestoneRepository(),
            new NullIssueRepository(),
            objectChatLinkRepository);

        return new ConvertConversationToSubtaskHandler(
            chatCoreClient,
            createSubtaskHandler,
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

    private sealed class RecordingSubtaskRepository : ISubtaskRepository
    {
        private readonly List<Subtask> _subtasks = new();

        public IReadOnlyList<Subtask> Subtasks => _subtasks;

        public Task AddAsync(Subtask domain, CancellationToken cancellationToken = default)
        {
            _subtasks.Add(domain);
            return Task.CompletedTask;
        }

        public Task<Subtask?> GetAsync(SubtaskId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_subtasks.FirstOrDefault(subtask => subtask.Id == id));

        public Task UpdateAsync(Subtask domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Subtask>> ListByTaskAsync(
            TaskId taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subtask>>(Array.Empty<Subtask>());
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
