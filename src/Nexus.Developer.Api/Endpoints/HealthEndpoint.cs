namespace Nexus.Developer.Api.Endpoints;

public static class HealthEndpoint
{
    public static IEndpointRouteBuilder MapHealthEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                status = "Healthy",
                service = "Nexus.Developer.Api",
                version = typeof(Program)
                    .Assembly
                    .GetName()
                    .Version?
                    .ToString()
            });
        });

        return endpoints;
    }
}
