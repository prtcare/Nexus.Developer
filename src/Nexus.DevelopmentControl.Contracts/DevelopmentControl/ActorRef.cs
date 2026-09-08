namespace Nexus.Developer.Core.DevelopmentControl;

// The small identity+name of whoever performed an action. Id is free text (a user id, a
// session user, an agent worker id); Name is the display name the workbook records
// ("Durai", "Codex", ...). Used by MutationEnvelope and by activity/reservation calls.
public sealed record ActorRef(
    ActorType Type,
    string Id,
    string Name);
