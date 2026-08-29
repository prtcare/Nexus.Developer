using Nexus.Developer.Application.Milestones.Commands.CreateMilestone;
using Nexus.Developer.Application.Scope;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.Scope;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class CreateMilestoneHandlerTests
{
    [Fact]
    public async Task Create_WhenSubprojectExists_CreatesMilestone()
    {
        var subprojectId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var targetDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var scopeClient = new FakeScopeClient(
            new ScopeSubproject(subprojectId, Guid.NewGuid(), "My Subproject", "SP-0001"));
        var repository = new RecordingMilestoneRepository();
        var handler = new CreateMilestoneHandler(scopeClient, repository);

        var result = await handler.HandleAsync(
            new CreateMilestoneCommand(
                new SubprojectId(subprojectId),
                Name: "New Milestone",
                Description: "A milestone",
                TargetDate: targetDate,
                CreatedByUserId: createdByUserId));

        var milestone = Assert.Single(repository.Milestones);

        // Result shape.
        Assert.Equal(milestone.Id, result.MilestoneId);
        Assert.Equal(milestone.Reference, result.Reference);
        Assert.Equal(milestone.Name, result.Name);

        // Milestone carried the command's fields.
        Assert.Equal(new SubprojectId(subprojectId), milestone.SubprojectId);
        Assert.Equal("New Milestone", milestone.Name);
        Assert.Equal("A milestone", milestone.Description);
        Assert.Equal(targetDate, milestone.TargetDate);
        Assert.Equal(createdByUserId, milestone.CreatedByUserId);
    }

    [Fact]
    public async Task Create_WhenSubprojectDoesNotExist_ThrowsAndCreatesNothing()
    {
        var subprojectId = Guid.NewGuid();
        var repository = new RecordingMilestoneRepository();
        var handler = new CreateMilestoneHandler(new FakeScopeClient(), repository);

        var ex = await Assert.ThrowsAsync<SubprojectNotFoundException>(() =>
            handler.HandleAsync(
                new CreateMilestoneCommand(
                    new SubprojectId(subprojectId),
                    Name: "Should not persist",
                    Description: string.Empty,
                    TargetDate: null,
                    CreatedByUserId: Guid.NewGuid())));

        Assert.Equal(new SubprojectId(subprojectId), ex.SubprojectId);
        Assert.Equal($"The subproject '{subprojectId}' does not exist.", ex.Message);
        Assert.Empty(repository.Milestones);
    }

    private sealed class FakeScopeClient : IScopeClient
    {
        private readonly IReadOnlyDictionary<SubprojectId, ScopeSubproject> _subprojects;

        public FakeScopeClient(params ScopeSubproject[] subprojects)
            => _subprojects = subprojects.ToDictionary(s => new SubprojectId(s.SubprojectId));

        public Task<ScopeSubproject?> GetSubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _subprojects.TryGetValue(subprojectId, out var subproject) ? subproject : null);
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
}
