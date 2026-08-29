using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Developer.Core.ChatCore;
using Nexus.Developer.Core.DevelopmentRuns;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.ObjectChatLinks;
using Nexus.Developer.Core.Scope;
using Nexus.Developer.Core.Subtasks;
using Nexus.Developer.Core.Tasks;
using Nexus.Developer.Infrastructure.ChatCore;
using Nexus.Developer.Infrastructure.Scope;
using Nexus.Developer.Infrastructure.Sql;
using Nexus.Developer.Infrastructure.Sql.Repositories;

namespace Nexus.Developer.Infrastructure;

// Wires the Infrastructure layer into a host's DI container: the
// NexusDeveloperDbContext (against the "NexusDeveloper" connection string) plus
// one Sql*Repository per Core repository interface. Mirrors the Chat
// Infrastructure's own AddChatInfrastructure-style registration extension.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeveloperInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NexusDeveloper")
            ?? throw new InvalidOperationException(
                "Connection string 'NexusDeveloper' is not configured.");

        services.AddDbContext<NexusDeveloperDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IFeatureRepository, SqlFeatureRepository>();
        services.AddScoped<ITaskRepository, SqlTaskRepository>();
        services.AddScoped<ISubtaskRepository, SqlSubtaskRepository>();
        services.AddScoped<IMilestoneRepository, SqlMilestoneRepository>();
        services.AddScoped<IMilestoneLinkRepository, SqlMilestoneLinkRepository>();
        services.AddScoped<IIssueRepository, SqlIssueRepository>();
        services.AddScoped<IIssueLinkRepository, SqlIssueLinkRepository>();
        services.AddScoped<IObjectChatLinkRepository, SqlObjectChatLinkRepository>();
        services.AddScoped<IDevelopmentRunRepository, SqlDevelopmentRunRepository>();

        var chatCoreBaseUrl = configuration.GetSection("ChatCoreApi")["BaseUrl"]
            ?? throw new InvalidOperationException(
                "ChatCoreApi:BaseUrl is not configured.");

        services.AddHttpClient<IChatCoreClient, HttpChatCoreClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(chatCoreBaseUrl);
        });

        // The Subproject endpoint is hosted on the same Nexus.Experience Chat Api
        // process as Chat Core's conversations (Nexus.Products.Chat.Api maps both),
        // so it reuses the same BaseUrl -- no second config key.
        services.AddHttpClient<IScopeClient, HttpScopeClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(chatCoreBaseUrl);
        });

        return services;
    }
}
