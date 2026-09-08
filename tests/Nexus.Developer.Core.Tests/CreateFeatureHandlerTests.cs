using Nexus.Developer.Application.Features;
using Nexus.Developer.Application.Features.Commands.CreateFeature;
using Nexus.Developer.Application.Scope;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Scope;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class CreateFeatureHandlerTests
{
    [Fact]
    public async Task Create_WhenSubprojectExists_CreatesFeature()
    {
        var subprojectId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var scopeClient = new FakeScopeClient(
            new ScopeSubproject(subprojectId, Guid.NewGuid(), "My Subproject", "SP-0001"));
        var repository = new RecordingFeatureRepository();
        var handler = new CreateFeatureHandler(scopeClient, repository);

        var result = await handler.HandleAsync(
            new CreateFeatureCommand(
                new SubprojectId(subprojectId),
                Title: "New Feature",
                Description: "A feature",
                CreatedByUserId: createdByUserId));

        var feature = Assert.Single(repository.Features);

        // Result shape.
        Assert.Equal(feature.Id, result.FeatureId);
        Assert.Equal(feature.Reference, result.Reference);
        Assert.Equal(feature.Title, result.Title);

        // Feature carried the command's fields.
        Assert.Equal(new SubprojectId(subprojectId), feature.SubprojectId);
        Assert.Equal("New Feature", feature.Title);
        Assert.Equal("A feature", feature.Description);
        Assert.Equal(createdByUserId, feature.CreatedByUserId);
    }

    [Fact]
    public async Task Create_WhenCallerSuppliesSourceRoadmapNodeId_SetsItOnFeature()
    {
        // WU-02 narrow bridge (WAVE-05A Lane D): the roadmap importer
        // (WI-07-1.1.3) will be a caller of the create contract, tagging the
        // Feature with the roadmap-ledger NodeId that originated it. Optional and
        // traceability-only -- the aggregate normalizes blank -> null and trims.
        var subprojectId = Guid.NewGuid();
        var scopeClient = new FakeScopeClient(
            new ScopeSubproject(subprojectId, Guid.NewGuid(), "My Subproject", "SP-0001"));
        var repository = new RecordingFeatureRepository();
        var handler = new CreateFeatureHandler(scopeClient, repository);

        var result = await handler.HandleAsync(
            new CreateFeatureCommand(
                new SubprojectId(subprojectId),
                Title: "New Feature",
                Description: "A feature",
                CreatedByUserId: Guid.NewGuid(),
                SourceRoadmapNodeId: "  F-07-10  "));

        var feature = Assert.Single(repository.Features);

        Assert.Equal("F-07-10", feature.SourceRoadmapNodeId);
        Assert.Equal(feature.Id, result.FeatureId);
    }

    [Fact]
    public async Task Create_WhenNoSourceRoadmapNodeIdSupplied_FeatureHasNone()
    {
        // The field must stay null for ordinary creates -- dormant until the
        // roadmap importer exists; no roadmap tag is ever fabricated.
        var subprojectId = Guid.NewGuid();
        var scopeClient = new FakeScopeClient(
            new ScopeSubproject(subprojectId, Guid.NewGuid(), "My Subproject", "SP-0001"));
        var repository = new RecordingFeatureRepository();
        var handler = new CreateFeatureHandler(scopeClient, repository);

        await handler.HandleAsync(
            new CreateFeatureCommand(
                new SubprojectId(subprojectId),
                Title: "New Feature",
                Description: "A feature",
                CreatedByUserId: Guid.NewGuid()));

        Assert.Null(Assert.Single(repository.Features).SourceRoadmapNodeId);
    }

    [Fact]
    public async Task Create_WhenSubprojectDoesNotExist_ThrowsAndCreatesNothing()
    {
        var subprojectId = Guid.NewGuid();
        var repository = new RecordingFeatureRepository();
        var handler = new CreateFeatureHandler(new FakeScopeClient(), repository);

        var ex = await Assert.ThrowsAsync<SubprojectNotFoundException>(() =>
            handler.HandleAsync(
                new CreateFeatureCommand(
                    new SubprojectId(subprojectId),
                    Title: "Should not persist",
                    Description: string.Empty,
                    CreatedByUserId: Guid.NewGuid())));

        Assert.Equal(new SubprojectId(subprojectId), ex.SubprojectId);
        Assert.Equal($"The subproject '{subprojectId}' does not exist.", ex.Message);
        Assert.Empty(repository.Features);
    }

    [Fact]
    public async Task Create_UnderExistingParentInSameSubproject_SetsParentFeatureId()
    {
        var subprojectId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var scopeClient = new FakeScopeClient(
            new ScopeSubproject(subprojectId, Guid.NewGuid(), "My Subproject", "SP-0001"));
        var repository = new RecordingFeatureRepository();
        var handler = new CreateFeatureHandler(scopeClient, repository);

        var parentResult = await handler.HandleAsync(
            new CreateFeatureCommand(
                new SubprojectId(subprojectId),
                Title: "Parent",
                Description: "A parent",
                CreatedByUserId: createdByUserId));
        var parentId = parentResult.FeatureId;

        var childResult = await handler.HandleAsync(
            new CreateFeatureCommand(
                new SubprojectId(subprojectId),
                Title: "Child",
                Description: "A subfeature",
                CreatedByUserId: createdByUserId,
                ParentFeatureId: parentId));

        var child = repository.Features.Single(feature => feature.Id == childResult.FeatureId);
        var parent = repository.Features.Single(feature => feature.Id == parentId);

        Assert.Equal(parent.Id, child.ParentFeatureId);
        Assert.Equal(parent.SubprojectId, child.SubprojectId);
        Assert.Null(parent.ParentFeatureId);
    }

    [Fact]
    public async Task Create_WhenParentDoesNotExist_ThrowsAndCreatesNothing()
    {
        var subprojectId = Guid.NewGuid();
        var scopeClient = new FakeScopeClient(
            new ScopeSubproject(subprojectId, Guid.NewGuid(), "My Subproject", "SP-0001"));
        var repository = new RecordingFeatureRepository();
        var handler = new CreateFeatureHandler(scopeClient, repository);
        var missingParentId = FeatureId.New();

        var ex = await Assert.ThrowsAsync<FeatureParentNotFoundException>(() =>
            handler.HandleAsync(
                new CreateFeatureCommand(
                    new SubprojectId(subprojectId),
                    Title: "Should not persist",
                    Description: string.Empty,
                    CreatedByUserId: Guid.NewGuid(),
                    ParentFeatureId: missingParentId)));

        Assert.Equal(missingParentId, ex.ParentFeatureId);
        Assert.Equal($"The parent feature '{missingParentId}' does not exist.", ex.Message);
        Assert.Empty(repository.Features);
    }

    [Fact]
    public async Task Create_WhenParentInDifferentSubproject_ThrowsAndCreatesNothing()
    {
        var subprojectId = Guid.NewGuid();
        var otherSubprojectId = Guid.NewGuid();
        var scopeClient = new FakeScopeClient(
            new ScopeSubproject(subprojectId, Guid.NewGuid(), "My Subproject", "SP-0001"),
            new ScopeSubproject(otherSubprojectId, Guid.NewGuid(), "Other Subproject", "SP-0002"));
        var parentInOtherSubproject = new Feature(
            FeatureId.New(),
            new SubprojectId(otherSubprojectId),
            "Parent",
            "parent in another subproject",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        var repository = new RecordingFeatureRepository(parentInOtherSubproject);
        var handler = new CreateFeatureHandler(scopeClient, repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new CreateFeatureCommand(
                    new SubprojectId(subprojectId),
                    Title: "Should not persist",
                    Description: string.Empty,
                    CreatedByUserId: Guid.NewGuid(),
                    ParentFeatureId: parentInOtherSubproject.Id)));

        var persisted = repository.Features.Single();
        Assert.Equal(parentInOtherSubproject.Id, persisted.Id);
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

    private sealed class RecordingFeatureRepository : IFeatureRepository
    {
        private readonly List<Feature> _features;

        public RecordingFeatureRepository(params Feature[] features)
            => _features = new List<Feature>(features);

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
}
