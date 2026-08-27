using Nexus.Developer.Api.Endpoints.Features;
using Nexus.Developer.Api.Endpoints.Issues;
using Nexus.Developer.Api.Endpoints.Milestones;
using Nexus.Developer.Api.Endpoints.Subtasks;
using Nexus.Developer.Api.Endpoints.Tasks;
using Nexus.Developer.Application;
using Nexus.Developer.Core.Scope;
using Nexus.Developer.Infrastructure;
using Nexus.ProductCore.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDeveloperInfrastructure(builder.Configuration);
builder.Services.AddDeveloperApplication();

// CHG-20260827-002 (M-06-1.2 Slice 3): a process-local scope-kind registry. Developer
// registers its own hierarchy below Layer 06's shared Subproject trunk kind. This does not
// yet resolve scope across processes (Developer/Product Core/Experience are separate hosts
// today) - see ScopeKindRegistry's remarks in Nexus.ProductCore.Contracts.
builder.Services.AddScopeKindRegistry();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

DeveloperScopeKinds.RegisterAll(app.Services.GetRequiredService<IScopeKindRegistry>());

app.MapFeatureEndpoints();
app.MapTaskEndpoints();
app.MapSubtaskEndpoints();
app.MapMilestoneEndpoints();
app.MapIssueEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based integration tests, mirroring the
// standard ASP.NET Core minimal-API testing convention.
public partial class Program;
