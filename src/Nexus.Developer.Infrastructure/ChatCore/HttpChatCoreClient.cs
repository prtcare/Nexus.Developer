using System.Net;
using System.Net.Http.Json;
using Nexus.Developer.Core.ChatCore;

namespace Nexus.Developer.Infrastructure.ChatCore;

// Typed HttpClient over Chat Core's (Nexus.Experience) minimal-API surface.
// BaseAddress is configured from the "ChatCoreApi:BaseUrl" configuration section
// in ServiceCollectionExtensions.
public sealed class HttpChatCoreClient : IChatCoreClient
{
    private readonly HttpClient _httpClient;

    public HttpChatCoreClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ChatCoreConversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/conversations/{conversationId}",
            cancellationToken);

        // 404 specifically means "conversation does not exist" -- the only
        // condition that maps to null. Anything else non-success (500, etc.)
        // must throw so it surfaces as a real infrastructure failure, not as a
        // missing conversation.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var conversation = await response.Content.ReadFromJsonAsync<ChatCoreConversation>(cancellationToken);

        if (conversation is null)
        {
            throw new InvalidOperationException(
                $"Chat Core returned an empty body for conversation '{conversationId}'.");
        }

        return conversation;
    }
}
