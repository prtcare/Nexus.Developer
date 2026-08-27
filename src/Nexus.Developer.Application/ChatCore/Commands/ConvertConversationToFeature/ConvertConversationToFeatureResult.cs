using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToFeature;

public sealed record ConvertConversationToFeatureResult(
    FeatureId FeatureId,
    string FeatureReference,
    string Title,
    ObjectChatLinkId ObjectChatLinkId,
    Guid ConversationId);
