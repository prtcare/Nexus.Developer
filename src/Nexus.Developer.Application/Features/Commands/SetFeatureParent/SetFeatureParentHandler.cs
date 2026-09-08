using Nexus.Developer.Application.Features;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;

namespace Nexus.Developer.Application.Features.Commands.SetFeatureParent;

// Re-parents an existing Feature (ParentFeatureId = null promotes it to root).
// The application write boundary is the enforcement point for the D04 hierarchy
// rules that need repository state: the parent must exist, and the child must not
// become an ancestor of its own ancestor. Self-parent and cross-subproject guards
// live in the aggregate's SetParent and are mirrored here so the cycle traversal
// never runs for a degenerate self-parent (the WorkItemDependency split).
public sealed class SetFeatureParentHandler
{
    private readonly IFeatureRepository _repository;

    public SetFeatureParentHandler(IFeatureRepository repository)
    {
        _repository = repository;
    }

    public async Task<SetFeatureParentResult> HandleAsync(
        SetFeatureParentCommand command,
        CancellationToken cancellationToken = default)
    {
        var child = await _repository.GetAsync(command.FeatureId, cancellationToken);

        if (child is null)
        {
            throw new FeatureNotFoundException(command.FeatureId);
        }

        if (command.ParentFeatureId is null)
        {
            child.SetParent(parent: null);
            await _repository.UpdateAsync(child, cancellationToken);
            return new SetFeatureParentResult(child.Id, child.ParentFeatureId);
        }

        var parent = await _repository.GetAsync(
            command.ParentFeatureId.Value,
            cancellationToken);

        if (parent is null)
        {
            throw new FeatureParentNotFoundException(command.ParentFeatureId.Value);
        }

        if (parent.Id == child.Id)
        {
            throw new ArgumentException(
                "A feature cannot be its own parent.",
                nameof(command.ParentFeatureId));
        }

        await EnsureNoCycleAsync(child, parent, cancellationToken);

        // The aggregate guard rejects a parent from a different Subproject (and
        // re-checks self) before mutating the child.
        child.SetParent(parent);

        await _repository.UpdateAsync(child, cancellationToken);

        return new SetFeatureParentResult(child.Id, child.ParentFeatureId);
    }

    // A cycle is formed exactly when the child is already an ancestor of the
    // candidate parent (walking ParentFeatureId up from the parent reaches the
    // child): setting the child's parent to that parent would then make the child
    // an ancestor of its own ancestor. The reported path closes back to the child.
    private async Task EnsureNoCycleAsync(
        Feature child,
        Feature parent,
        CancellationToken cancellationToken)
    {
        var ancestors = new List<FeatureId>();
        var current = parent;

        while (current is not null)
        {
            if (current.Id == child.Id)
            {
                var path = new List<FeatureId> { child.Id };
                path.AddRange(ancestors);
                path.Add(child.Id);

                throw new FeatureHierarchyCycleException(path);
            }

            ancestors.Add(current.Id);

            if (current.ParentFeatureId is null)
            {
                return;
            }

            current = await _repository.GetAsync(current.ParentFeatureId.Value, cancellationToken);

            if (current is null)
            {
                // The self-FK (Restrict) makes a dangling parent impossible from
                // SQL; a defensively null row simply ends the walk.
                return;
            }
        }
    }
}
