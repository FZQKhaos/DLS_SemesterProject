using System.Net.Http.Json;
using CommentService.Client.Dto;
using CommentService.Client.Interface;
using Polly.CircuitBreaker;

namespace CommentService.Client;

public sealed class ProfanityClient(HttpClient httpClient, ILogger<ProfanityClient> logger) : IProfanityClient
{
    public async Task<ProfanityFilterResponse> FilterAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/profanity/filter",
                new ProfanityFilterRequest { Text = text },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ProfanityService returned HTTP {StatusCode}.", response.StatusCode);
                return ProfanityFilterResponse.Unavailable();
            }

            return await response.Content.ReadFromJsonAsync<ProfanityFilterResponse>(cancellationToken: cancellationToken)
                   ?? ProfanityFilterResponse.Unavailable();
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "ProfanityService circuit is open; skipping remote call.");
            return ProfanityFilterResponse.Unavailable();
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "ProfanityService call timed out.");
            return ProfanityFilterResponse.Unavailable();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "ProfanityService is unreachable.");
            return ProfanityFilterResponse.Unavailable();
        }
    }
}
