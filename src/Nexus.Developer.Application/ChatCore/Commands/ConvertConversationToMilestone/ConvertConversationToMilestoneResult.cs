using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToMilestone;

public sealed record ConvertConversationToMilestoneResult(
    MilestoneId MilestoneId,
    string MilestoneReference,
    string Name,
    ObjectChatLinkId ObjectChatLinkId,
    Guid ConversationId);
