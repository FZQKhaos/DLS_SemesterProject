using System.Net.Http.Json;
using Polly.CircuitBreaker;

namespace NewsletterService.Client;

public sealed class SubscriberClient(HttpClient httpClient, ILogger<SubscriberClient> logger) : ISubscriberClient
{
    public async Task<IReadOnlyList<SubscriberDto>?> GetSubscribersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("api/subscribers", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SubscriberService returned HTTP {StatusCode}.", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<List<SubscriberDto>>(cancellationToken: cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "SubscriberService circuit is open; skipping remote call.");
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "SubscriberService call timed out.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "SubscriberService is unreachable.");
            return null;
        }
    }
}
