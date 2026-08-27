using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToIssue;
using Nexus.Developer.Application.Issues.Commands.CreateIssue;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
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

public class ConvertConversationToIssueHandlerTests
{
    [Fact]
    public async Task Convert_WhenConversationExists_CreatesIssueAndLink()
    {
        var conversationId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat about the issue", DateTimeOffset.UtcNow));
        var issueRepo = new RecordingIssueRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(chatCoreClient, issueRepo, linkRepo);

        var command = new ConvertConversationToIssueCommand(
            ConversationId: conversationId,
            Title: "Converted Issue",
            Description: "From chat",
            CreatedByUserId: createdByUserId,
            MessageRangeStart: null,
            MessageRangeEnd: null);

        var result = await handler.HandleAsync(command);

        var issue = Assert.Single(issueRepo.Issues);
        var link = Assert.Single(linkRepo.Links);

        // Result shape.
        Assert.Equal(issue.Id, result.IssueId);
        Assert.Equal(issue.Reference, result.IssueReference);
        Assert.Equal(issue.Title, result.Title);
        Assert.Equal(link.Id, result.ObjectChatLinkId);
        Assert.Equal(conversationId, result.ConversationId);

        // Issue carried the command's fields; CreatedByUserId doubles as creator.
        Assert.Equal("Converted Issue", issue.Title);
        Assert.Equal("From chat", issue.Description);
        Assert.Equal(createdByUserId, issue.CreatedByUserId);

        // Link points back at the new Issue and the originating conversation.
        Assert.Equal(conversationId, link.ConversationId);
        Assert.Equal(ObjectChatLinkTargetType.Issue, link.TargetType);
        Assert.Equal(issue.Id.Value, link.TargetId);
        Assert.Equal(createdByUserId, link.LinkedByUserId);
    }

    [Fact]
    public async Task Convert_WhenConversationDoesNotExist_ThrowsAndCreatesNothing()
    {
        var conversationId = Guid.NewGuid();
        var issueRepo = new RecordingIssueRepository();
        var linkRepo = new RecordingObjectChatLinkRepository();
        var handler = CreateHandler(new FakeChatCoreClient(), issueRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<ConversationNotFoundException>(() =>
            handler.HandleAsync(
                new ConvertConversationToIssueCommand(
                    ConversationId: conversationId,
                    Title: "Should not run",
                    Description: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        Assert.Equal(conversationId, ex.ConversationId);
        Assert.Empty(issueRepo.Issues);
        Assert.Empty(linkRepo.Links);
    }

    [Fact]
    public async Task Convert_WhenLinkCreationFailsAfterIssueCreated_IssuePersistsAndExceptionPropagates()
    {
        var conversationId = Guid.NewGuid();
        var chatCoreClient = new FakeChatCoreClient(
            new ChatCoreConversation(conversationId, "Chat", DateTimeOffset.UtcNow));
        var issueRepo = new RecordingIssueRepository();
        // Link persistence fails after the Issue above has already been saved.
        var linkRepo = new RecordingObjectChatLinkRepository(throwOnAdd: true);
        var handler = CreateHandler(chatCoreClient, issueRepo, linkRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new ConvertConversationToIssueCommand(
                    ConversationId: conversationId,
                    Title: "Issue with no link",
                    Description: null,
                    CreatedByUserId: Guid.NewGuid(),
                    MessageRangeStart: null,
                    MessageRangeEnd: null)));

        // Known limitation of this slice, asserted as a test: the Issue exists,
        // unlinked, and the failure surfaced -- no compensating delete.
        Assert.Equal("Simulated chat-link persistence failure.", ex.Message);
        Assert.Single(issueRepo.Issues);
        Assert.Empty(linkRepo.Links);
    }

    private static ConvertConversationToIssueHandler CreateHandler(
        IChatCoreClient chatCoreClient,
        IIssueRepository issueRepository,
        IObjectChatLinkRepository objectChatLinkRepository)
    {
        var createIssueHandler = new CreateIssueHandler(issueRepository);
        var createObjectChatLinkHandler = new CreateObjectChatLinkHandler(
            new NullFeatureRepository(),
            new NullTaskRepository(),
            new NullSubtaskRepository(),
            new NullMilestoneRepository(),
            issueRepository,
            objectChatLinkRepository);

        return new ConvertConversationToIssueHandler(
            chatCoreClient,
            createIssueHandler,
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

    private sealed class RecordingIssueRepository : IIssueRepository
    {
        private readonly List<Issue> _issues = new();

        public IReadOnlyList<Issue> Issues => _issues;

        public Task AddAsync(Issue domain, CancellationToken cancellationToken = default)
        {
            _issues.Add(domain);
            return Task.CompletedTask;
        }

        public Task<Issue?> GetAsync(IssueId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_issues.FirstOrDefault(issue => issue.Id == id));

        public Task UpdateAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
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
}
