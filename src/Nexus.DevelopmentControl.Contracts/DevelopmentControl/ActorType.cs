namespace Nexus.Developer.Core.DevelopmentControl;

// Who performed an action. The workbook's vocabulary evidences exactly two kinds: the
// human requester/decision-maker (the "Requested By" column holds names like "Durai";
// AGENTS.md mandates a recorded human review) and the coding agent doing the work (the
// "Worker" column holds names like "Codex"/"Claude"/"ChatGPT"). No third actor kind is
// evidenced anywhere, so none is invented.
public enum ActorType
{
    Human = 1,
    Agent = 2
}
