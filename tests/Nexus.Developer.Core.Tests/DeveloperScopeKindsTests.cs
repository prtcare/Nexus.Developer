using Nexus.Developer.Core.Scope;
using Nexus.ProductCore.Contracts;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public sealed class DeveloperScopeKindsTests
{
    [Fact]
    public void RegisterAll_RegistersAllFourKinds()
    {
        var registry = new ScopeKindRegistry();

        DeveloperScopeKinds.RegisterAll(registry);

        Assert.True(registry.IsRegistered(DeveloperScopeKinds.Feature));
        Assert.True(registry.IsRegistered(DeveloperScopeKinds.Task));
        Assert.True(registry.IsRegistered(DeveloperScopeKinds.Subtask));
        Assert.True(registry.IsRegistered(DeveloperScopeKinds.Milestone));
    }

    [Fact]
    public void RegisterAll_NestsFeatureAndMilestoneDirectlyBelowSubproject()
    {
        var registry = new ScopeKindRegistry();

        DeveloperScopeKinds.RegisterAll(registry);

        var feature = registry.All.Single(r => r.Kind == DeveloperScopeKinds.Feature);
        var milestone = registry.All.Single(r => r.Kind == DeveloperScopeKinds.Milestone);

        Assert.Equal(WellKnownScopeKinds.Subproject, feature.ParentKind);
        Assert.Equal(WellKnownScopeKinds.Subproject, milestone.ParentKind);
    }

    [Fact]
    public void RegisterAll_NestsTaskUnderFeatureAndSubtaskUnderTask()
    {
        var registry = new ScopeKindRegistry();

        DeveloperScopeKinds.RegisterAll(registry);

        var task = registry.All.Single(r => r.Kind == DeveloperScopeKinds.Task);
        var subtask = registry.All.Single(r => r.Kind == DeveloperScopeKinds.Subtask);

        Assert.Equal(DeveloperScopeKinds.Feature, task.ParentKind);
        Assert.Equal(DeveloperScopeKinds.Task, subtask.ParentKind);
    }

    [Fact]
    public void RegisterAll_DoesNotRegisterIssue()
    {
        // Deliberate: Issue's many-target IssueLink attachment does not fit
        // ScopeKindRegistration's single-parent-kind model. See DeveloperScopeKinds' remarks.
        var registry = new ScopeKindRegistry();

        DeveloperScopeKinds.RegisterAll(registry);

        Assert.False(registry.IsRegistered(new ScopeKind("Issue")));
    }
}
