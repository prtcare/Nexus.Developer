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
}
