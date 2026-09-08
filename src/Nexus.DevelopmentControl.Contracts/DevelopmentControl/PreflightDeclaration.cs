namespace Nexus.Developer.Core.DevelopmentControl;

// The mandatory-preflight declaration AGENTS.md requires before any edit: change id,
// roadmap node id, repositories/projects, files/globs, schema/DbContext mutation,
// public contracts/APIs, dependencies, risk, worker, branch and sibling worktree.
// RunPreflightAsync compares this against every change whose status is not Completed or
// Cancelled and returns exactly one verdict.
public sealed record PreflightDeclaration(
    string ChangeId,
    NodeId RoadmapNodeId,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> FilesGlobs,
    bool SchemaOrDbContextMutation,
    IReadOnlyList<string> ContractsApis,
    IReadOnlyList<string> Dependencies,
    string Risk,
    string Worker,
    string? Branch,
    string? SiblingWorktree);
