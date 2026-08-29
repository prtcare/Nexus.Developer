using System.Net;
using System.Net.Http.Json;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Scope;

namespace Nexus.Developer.Infrastructure.Scope;

// Typed HttpClient over Nexus.Experience's (Product Core's) minimal-API subproject
// surface. BaseAddress is configured from the same "ChatCoreApi:BaseUrl"
// configuration section as HttpChatCoreClient in ServiceCollectionExtensions: the
// subproject endpoint is hosted on the same Nexus.Experience Chat Api process
// (Nexus.Products.Chat.Api, http://localhost:5095) as Chat Core's conversations.
public sealed class HttpScopeClient : IScopeClient
{
    private readonly HttpClient _httpClient;

    public HttpScopeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ScopeSubproject?> GetSubprojectAsync(
        SubprojectId subprojectId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/subprojects/{subprojectId.Value}",
            cancellationToken);

        // 404 specifically means "subproject does not exist" -- the only
        // condition that maps to null. Anything else non-success (500, etc.)
        // must throw so it surfaces as a real infrastructure failure, not as a
        // missing subproject.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var subproject = await response.Content.ReadFromJsonAsync<ScopeSubproject>(cancellationToken);

        if (subproject is null)
        {
            throw new InvalidOperationException(
                $"Product Core returned an empty body for subproject '{subprojectId.Value}'.");
        }

        return subproject;
    }
}
