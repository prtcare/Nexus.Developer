using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns;

// Thrown by CreateDevelopmentRunHandler when the Feature/Task/Issue the run
// targets cannot be resolved (WI-07-10.3.1): the endpoint maps this to 404 Not
// Found rather than letting it surface as an unhandled 500. Mirrors
// ObjectChatLinkTargetNotFoundException, but a DevelopmentRun's target is its
// subject, not the far end of a link, so the message carries no chat-link suffix.
public sealed class DevelopmentRunTargetNotFoundException : Exception
{
    public DevelopmentRunTargetNotFoundException(
        DevelopmentRunTargetType targetType,
        Guid targetId)
        : base($"The {targetType} '{targetId}' does not exist.")
    {
        TargetType = targetType;
        TargetId = targetId;
    }

    public DevelopmentRunTargetType TargetType { get; }

    public Guid TargetId { get; }
}
