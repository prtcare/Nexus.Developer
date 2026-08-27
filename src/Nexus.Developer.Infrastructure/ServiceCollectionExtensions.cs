using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Developer.Core.DevelopmentRuns;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.ObjectChatLinks;
using Nexus.Developer.Core.Subtasks;
using Nexus.Developer.Core.Tasks;
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

        return services;
    }
}
