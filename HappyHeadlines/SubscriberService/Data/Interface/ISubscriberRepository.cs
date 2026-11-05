using SubscriberService.Domain;

namespace SubscriberService.Data.Interface;

public interface ISubscriberRepository
{
    /// <returns>The stored subscriber, or null if the email already exists.</returns>
    Task<Subscriber?> AddAsync(Subscriber subscriber, CancellationToken cancellationToken = default);

    /// <returns>true if a subscriber was removed.</returns>
    Task<bool> RemoveByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subscriber>> GetAllAsync(CancellationToken cancellationToken = default);
}
