using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using Events;

namespace NewsletterService.Worker;

/// <summary>
/// Listens on SubscriberQueue and "sends" a welcome mail to new subscribers.
/// Has its OWN bus connection (SubscriberQueue is a different broker than the ArticlePublished one)
/// and reconnects on its own: if the subscriber lane is down, NewsletterService still starts.
/// </summary>
public sealed class SubscriberSubscribedConsumer(
    IConfiguration configuration,
    ILogger<SubscriberSubscribedConsumer> logger) : BackgroundService
{
    private const string SubscriptionId = "NewsletterService";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["SubscriberQueue:ConnectionString"]
            ?? "host=localhost;port=5673;username=appuser;password=apppassword";

        while (!stoppingToken.IsCancellationRequested)
        {
            IBus? bus = null;
            try
            {
                bus = RabbitHutch.CreateBus(
                    connectionString,
                    serviceRegister => serviceRegister.EnableSystemTextJson());

                using var subscription = await bus.PubSub.SubscribeAsync<SubscriberSubscribed>(
                    SubscriptionId,
                    HandleMessageAsync,
                    _ => { },
                    cancellationToken: stoppingToken);

                logger.LogInformation("Listening on SubscriberQueue.");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // application is shutting down
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SubscriberQueue is unavailable. Retrying in {Seconds} seconds.", RetryDelay.TotalSeconds);
                try
                {
                    await Task.Delay(RetryDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // application is shutting down
                }
            }
            finally
            {
                bus?.Dispose();
            }
        }
    }

    private Task HandleMessageAsync(SubscriberSubscribed message, CancellationToken cancellationToken)
    {
        // The message was already accepted by SubscriberService, so we process it even if the
        // toggle was switched off in the meantime (no lost welcome mails).
        logger.LogInformation("Welcome mail sent to new subscriber {SubscriberId}.", message.Id);
        return Task.CompletedTask;
    }
}
