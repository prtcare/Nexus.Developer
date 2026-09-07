using Nexus.Developer.Application.Features;
using Nexus.Developer.Application.Features.Commands.SetFeatureParent;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class SetFeatureParentHandlerTests
{
    [Fact]
    public async Task Reparent_ToAnotherRootInSameSubproject_UpdatesParent()
    {
        var subprojectId = SubprojectId.New();
        var by = Guid.NewGuid();
        var child = Feature(subprojectId, "Child", by, parentId: null);
        var newParent = Feature(subprojectId, "Parent", by, parentId: null);
        var repository = new InMemoryFeatureRepository(child, newParent);
        var handler = new SetFeatureParentHandler(repository);

        var result = await handler.HandleAsync(
            new SetFeatureParentCommand(child.Id, newParent.Id));

        Assert.Equal(newParent.Id, result.ParentFeatureId);
        Assert.Equal(newParent.Id, child.ParentFeatureId);
    }

    [Fact]
    public async Task PromoteChildToRoot_ClearsParent()
    {
        var subprojectId = SubprojectId.New();
        var by = Guid.NewGuid();
        var root = Feature(subprojectId, "Root", by, parentId: null);
        var child = Feature(subprojectId, "Child", by, parentId: root.Id);
        var repository = new InMemoryFeatureRepository(root, child);
        var handler = new SetFeatureParentHandler(repository);

        var result = await handler.HandleAsync(
            new SetFeatureParentCommand(child.Id, ParentFeatureId: null));

        Assert.Null(result.ParentFeatureId);
        Assert.Null(child.ParentFeatureId);
    }

    [Fact]
    public async Task Reparent_WhenChildDoesNotExist_Throws()
    {
        var missingId = FeatureId.New();
        var handler = new SetFeatureParentHandler(new InMemoryFeatureRepository());

        var ex = await Assert.ThrowsAsync<FeatureNotFoundException>(() =>
            handler.HandleAsync(new SetFeatureParentCommand(missingId, ParentFeatureId: null)));

        Assert.Equal(missingId, ex.FeatureId);
        Assert.Equal($"The feature '{missingId}' does not exist.", ex.Message);
    }

    [Fact]
    public async Task Reparent_WhenParentDoesNotExist_ThrowsAndDoesNotChange()
    {
        var subprojectId = SubprojectId.New();
        var child = Feature(subprojectId, "Child", Guid.NewGuid(), parentId: null);
        var missingParentId = FeatureId.New();
        var repository = new InMemoryFeatureRepository(child);
        var handler = new SetFeatureParentHandler(repository);

        var ex = await Assert.ThrowsAsync<FeatureParentNotFoundException>(() =>
            handler.HandleAsync(new SetFeatureParentCommand(child.Id, missingParentId)));

        Assert.Equal(missingParentId, ex.ParentFeatureId);
        Assert.Null(child.ParentFeatureId);
    }

    [Fact]
    public async Task Reparent_Self_ThrowsAndDoesNotChange()
    {
        var subprojectId = SubprojectId.New();
        var feature = Feature(subprojectId, "F", Guid.NewGuid(), parentId: null);
        var repository = new InMemoryFeatureRepository(feature);
        var handler = new SetFeatureParentHandler(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new SetFeatureParentCommand(feature.Id, feature.Id)));

        Assert.Null(feature.ParentFeatureId);
    }

    [Fact]
    public async Task Reparent_ToParentInDifferentSubproject_ThrowsAndDoesNotChange()
    {
        var subprojectA = SubprojectId.New();
        var subprojectB = SubprojectId.New();
        var child = Feature(subprojectA, "Child", Guid.NewGuid(), parentId: null);
        var otherSubprojectFeature = Feature(subprojectB, "Parent", Guid.NewGuid(), parentId: null);
        var repository = new InMemoryFeatureRepository(child, otherSubprojectFeature);
        var handler = new SetFeatureParentHandler(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new SetFeatureParentCommand(child.Id, otherSubprojectFeature.Id)));

        Assert.Null(child.ParentFeatureId);
    }

    [Fact]
    public async Task Reparent_DeepCycle_ThrowsNamingCycleAndDoesNotChange()
    {
        // Build A (root) <- B <- C, then attempt to make A a child of C: A would
        // become an ancestor of its own ancestor (A -> C -> B -> A).
        var subprojectId = SubprojectId.New();
        var by = Guid.NewGuid();
        var a = Feature(subprojectId, "A", by, parentId: null);
        var b = Feature(subprojectId, "B", by, parentId: a.Id);
        var c = Feature(subprojectId, "C", by, parentId: b.Id);
        var repository = new InMemoryFeatureRepository(a, b, c);
        var handler = new SetFeatureParentHandler(repository);

        var ex = await Assert.ThrowsAsync<FeatureHierarchyCycleException>(() =>
            handler.HandleAsync(new SetFeatureParentCommand(a.Id, c.Id)));

        Assert.Equal(new[] { a.Id, c.Id, b.Id, a.Id }, ex.Path);
        Assert.Contains($"Feature:{a.Id.Value}", ex.Message);
        Assert.Contains($"Feature:{c.Id.Value}", ex.Message);
        Assert.Contains($"Feature:{b.Id.Value}", ex.Message);
        Assert.Null(a.ParentFeatureId);
    }

    private static Feature Feature(
        SubprojectId subprojectId,
        string title,
        Guid createdByUserId,
        FeatureId? parentId)
        => new(
            FeatureId.New(),
            subprojectId,
            title,
            "d",
            createdByUserId,
            DateTimeOffset.UtcNow,
            parentId);

    private sealed class InMemoryFeatureRepository : IFeatureRepository
    {
        private readonly List<Feature> _features;

        public InMemoryFeatureRepository(params Feature[] features)
            => _features = new List<Feature>(features);

        public IReadOnlyList<Feature> Features => _features;

        public Task AddAsync(Feature domain, CancellationToken cancellationToken = default)
        {
            _features.Add(domain);
            return Task.CompletedTask;
        }

        public Task<Feature?> GetAsync(
            FeatureId id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_features.FirstOrDefault(feature => feature.Id == id));

        public Task UpdateAsync(Feature domain, CancellationToken cancellationToken = default)
        {
            var index = _features.FindIndex(feature => feature.Id == domain.Id);

            if (index >= 0)
            {
                _features[index] = domain;
            }
            else
            {
                _features.Add(domain);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Feature>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Feature>>(
                _features.Where(feature => feature.SubprojectId == subprojectId).ToList());
    }
}
