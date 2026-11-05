namespace NewsletterService.Client;

public interface ISubscriberClient
{
    /// <returns>
    /// The list of subscribers (may be empty), or <c>null</c> if SubscriberService
    /// could not be reached (timeout, open circuit breaker, error).
    /// null means "unknown"; an empty list means "no subscribers".
    /// </returns>
    Task<IReadOnlyList<SubscriberDto>?> GetSubscribersAsync(CancellationToken cancellationToken = default);
}
