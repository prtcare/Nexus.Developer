using Nexus.Developer.Api.Endpoints;
using Nexus.Developer.Api.Endpoints.ChatCore;
using Nexus.Developer.Api.Endpoints.Features;
using Nexus.Developer.Api.Endpoints.Issues;
using Nexus.Developer.Api.Endpoints.Milestones;
using Nexus.Developer.Api.Endpoints.ObjectChatLinks;
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

// CORS for the browser client (Nexus.Experience Vite dev server on :5173).
// Mirrors Nexus.Products.Chat.Api's Nexus:Cors:AllowedOrigins pattern so the
// convert-from-chat flow's cross-origin POSTs pass preflight in development.
var allowedOrigins =
    builder.Configuration
        .GetSection("Nexus:Cors:AllowedOrigins")
        .Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("NexusWebDevelopment", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("NexusWebDevelopment");

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
app.MapObjectChatLinkEndpoints();
app.MapConvertConversationEndpoints();
app.MapConvertConversationToTaskEndpoints();
app.MapConvertConversationToSubtaskEndpoints();
app.MapConvertConversationToMilestoneEndpoints();
app.MapConvertConversationToIssueEndpoints();
app.MapHealthEndpoint();

app.Run();

// Exposed for WebApplicationFactory-based integration tests, mirroring the
// standard ASP.NET Core minimal-API testing convention.
public partial class Program;
