using System.Net.Http.Json;
using CommentService.Clients.Dtos;
using Polly.CircuitBreaker;

namespace CommentService.Clients;

/// <summary>
/// The one place CommentService calls ProfanityService directly over
/// HTTP - no gateway or UI sits between the two services (see
/// Program.cs for how this HttpClient and its circuit breaker are wired
/// up). If ProfanityService is slow or down, this is also where the
/// circuit breaker "takes over": instead of letting every comment
/// request hang or fail, it falls back to letting the comment through
/// unchecked and flags it as such (see Comment.ProfanityChecked).
/// </summary>
public class ProfanityServiceClient(HttpClient httpClient, ILogger<ProfanityServiceClient> logger)
    : IProfanityServiceClient
{
    public async Task<ProfanityCheckResult> CheckAsync(string text)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/profanity/check", new ProfanityCheckRequest { Text = text });

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "ProfanityService responded with {StatusCode} - letting the comment through unchecked.",
                    response.StatusCode);
                return new ProfanityCheckResult(IsProfane: false, Checked: false);
            }

            var result = await response.Content.ReadFromJsonAsync<ProfanityCheckResponse>();
            return new ProfanityCheckResult(IsProfane: result?.IsProfane ?? false, Checked: true);
        }
        catch (BrokenCircuitException)
        {
            // The circuit breaker has already seen enough failures and is now
            // failing fast without even attempting the call - CommentService
            // stops hammering a service that's already down and falls back
            // immediately instead of waiting on a timeout every time.
            logger.LogWarning("ProfanityService circuit breaker is open - letting the comment through unchecked.");
            return new ProfanityCheckResult(IsProfane: false, Checked: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Could not reach ProfanityService - letting the comment through unchecked.");
            return new ProfanityCheckResult(IsProfane: false, Checked: false);
        }
    }
}
