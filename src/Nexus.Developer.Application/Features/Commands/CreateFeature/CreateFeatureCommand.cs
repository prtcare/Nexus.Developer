using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features.Commands.CreateFeature;

public sealed record CreateFeatureCommand(
    SubprojectId SubprojectId,
    string Title,
    string Description,
    Guid CreatedByUserId);
