using SubscriberService.Service.Dto;

namespace SubscriberService.Service.Interface;

public interface ISubscriptionService
{
    /// <returns>The new subscriber, or null if the email is already subscribed.</returns>
    Task<SubscriberResponseDto?> SubscribeAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> UnsubscribeAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriberResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
