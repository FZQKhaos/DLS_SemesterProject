using EasyNetQ;
using Events;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace SubscriberService;

// Independent asynchronous transaction: ArticlePublisher never waits for subscribers.
public sealed class ArticleFanoutWorker(
    IBus articleBus, SubscriberQueueBus subscriberQueue, NpgsqlDataSource database,
    RuntimeSubscriberToggle toggle, ILogger<ArticleFanoutWorker> logger) : BackgroundService
{
    private IDisposable? subscription;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                subscription = await articleBus.PubSub.SubscribeAsync<ArticlePublished>(
                    "SubscriberServiceArticleFanout", HandleArticleAsync,
                    _ => { }, cancellationToken: stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Article subscription unavailable; retrying without stopping subscriber API");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally { subscription?.Dispose(); }
        }
    }

    private async Task HandleArticleAsync(ArticlePublished article, CancellationToken ct)
    {
        if (!await toggle.IsEnabledAsync()) return;
        await using var connection = await database.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT email FROM subscribers ORDER BY email", connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var email = reader.GetString(0);
            await subscriberQueue.Bus.PubSub.PublishAsync(new SubscriberNotification
            {
                NotificationId = $"article:{article.Id}:{email}",
                Email = email,
                Subject = article.Title,
                Body = article.Body,
                Kind = "article"
            });
        }
    }
}
