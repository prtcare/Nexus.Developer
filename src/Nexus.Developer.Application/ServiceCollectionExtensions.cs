using Microsoft.Extensions.DependencyInjection;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToFeature;
using Nexus.Developer.Application.Features.Commands.CreateFeature;
using Nexus.Developer.Application.Features.Queries.GetFeature;
using Nexus.Developer.Application.Features.Queries.ListFeaturesBySubproject;
using Nexus.Developer.Application.Issues.Commands.CreateIssue;
using Nexus.Developer.Application.Issues.Commands.LinkIssue;
using Nexus.Developer.Application.Issues.Queries.GetIssue;
using Nexus.Developer.Application.Milestones.Commands.CreateMilestone;
using Nexus.Developer.Application.Milestones.Commands.LinkMilestone;
using Nexus.Developer.Application.Milestones.Queries.GetMilestone;
using Nexus.Developer.Application.Milestones.Queries.ListMilestonesBySubproject;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByConversation;
using Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByTarget;
using Nexus.Developer.Application.Subtasks.Commands.CreateSubtask;
using Nexus.Developer.Application.Subtasks.Queries.GetSubtask;
using Nexus.Developer.Application.Subtasks.Queries.ListSubtasksByTask;
using Nexus.Developer.Application.Tasks.Commands.CreateTask;
using Nexus.Developer.Application.Tasks.Queries.GetTask;
using Nexus.Developer.Application.Tasks.Queries.ListTasksByFeature;

namespace Nexus.Developer.Application;

// Registers every Create/Get/List/Link handler as scoped -- each handler holds
// only a repository dependency, so scoped (one instance per request) matches the
// DbContext lifetime registered by Nexus.Developer.Infrastructure.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeveloperApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateFeatureHandler>();
        services.AddScoped<GetFeatureHandler>();
        services.AddScoped<ListFeaturesBySubprojectHandler>();

        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<GetTaskHandler>();
        services.AddScoped<ListTasksByFeatureHandler>();

        services.AddScoped<CreateSubtaskHandler>();
        services.AddScoped<GetSubtaskHandler>();
        services.AddScoped<ListSubtasksByTaskHandler>();

        services.AddScoped<CreateMilestoneHandler>();
        services.AddScoped<GetMilestoneHandler>();
        services.AddScoped<ListMilestonesBySubprojectHandler>();
        services.AddScoped<LinkMilestoneHandler>();

        services.AddScoped<CreateIssueHandler>();
        services.AddScoped<GetIssueHandler>();
        services.AddScoped<LinkIssueHandler>();

        services.AddScoped<CreateObjectChatLinkHandler>();
        services.AddScoped<ListObjectChatLinksByConversationHandler>();
        services.AddScoped<ListObjectChatLinksByTargetHandler>();

        services.AddScoped<ConvertConversationToFeatureHandler>();

        return services;
    }
}
