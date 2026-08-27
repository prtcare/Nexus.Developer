using Nexus.Developer.Api.Endpoints.Features;
using Nexus.Developer.Api.Endpoints.Issues;
using Nexus.Developer.Api.Endpoints.Milestones;
using Nexus.Developer.Api.Endpoints.Subtasks;
using Nexus.Developer.Api.Endpoints.Tasks;
using Nexus.Developer.Application;
using Nexus.Developer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDeveloperInfrastructure(builder.Configuration);
builder.Services.AddDeveloperApplication();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapFeatureEndpoints();
app.MapTaskEndpoints();
app.MapSubtaskEndpoints();
app.MapMilestoneEndpoints();
app.MapIssueEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based integration tests, mirroring the
// standard ASP.NET Core minimal-API testing convention.
public partial class Program;
