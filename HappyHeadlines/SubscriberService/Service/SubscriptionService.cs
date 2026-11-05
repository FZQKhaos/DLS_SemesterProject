using EasyNetQ;
using Events;
using SubscriberService.Data.Interface;
using SubscriberService.Domain;
using SubscriberService.Service.Dto;
using SubscriberService.Service.Interface;

namespace SubscriberService.Service;

public sealed class SubscriptionService(
    ISubscriberRepository repository,
    IBus bus,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    // If SubscriberQueue is down, a subscription must not hang: wait at most 2 seconds.
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(2);

    public async Task<SubscriberResponseDto?> SubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        var subscriber = new Subscriber
        {
            Id = Guid.NewGuid().ToString(),
            Email = email.Trim().ToLowerInvariant(),
            SubscribedAtUtc = DateTime.UtcNow
        };

        var created = await repository.AddAsync(subscriber, cancellationToken);
        if (created is null)
        {
            return null; // already subscribed
        }

        await PublishSafelyAsync(created, cancellationToken);
        logger.LogInformation("Subscriber {Id} subscribed.", created.Id);
        return ToDto(created);
    }

    public async Task<bool> UnsubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        return await repository.RemoveByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriberResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var subscribers = await repository.GetAllAsync(cancellationToken);
        return subscribers.Select(ToDto).ToList();
    }

    private async Task PublishSafelyAsync(Subscriber subscriber, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PublishTimeout);

        try
        {
            await bus.PubSub.PublishAsync(
                new SubscriberSubscribed
                {
                    Id = subscriber.Id,
                    Email = subscriber.Email,
                    SubscribedAtUtc = subscriber.SubscribedAtUtc
                },
                timeout.Token);
        }
        catch (Exception ex)
        {
            // The subscriber IS stored. The queue is down or slow: log and move on (discard).
            logger.LogError(ex, "Subscriber {Id} stored, but publishing to SubscriberQueue failed.", subscriber.Id);
        }
    }

    private static SubscriberResponseDto ToDto(Subscriber subscriber) =>
        new(subscriber.Id, subscriber.Email, subscriber.SubscribedAtUtc);
}
