using System.Diagnostics;
using System.Text.Json;
using EasyNetQ;
using Events;
using FeatureToggles;
using Microsoft.Extensions.DependencyInjection;
using Monitoring;
using NewsletterService.Client;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace NewsletterService.Worker;

public sealed class ArticlePublishedSubscriber(
    IBus bus,
    IServiceScopeFactory scopeFactory,
    IFeatureToggles toggles,
    ILogger<ArticlePublishedSubscriber> logger) : IHostedService
{
    private const string SubscriptionId = "NewsletterService";
    private static readonly TraceContextPropagator Propagator = new();
    private IDisposable? subscription;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        subscription = await bus.PubSub.SubscribeAsync<ArticlePublished>(
            SubscriptionId,
            HandleMessageAsync,
            _ => { },
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        subscription?.Dispose();
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(ArticlePublished message, CancellationToken cancellationToken)
    {
        var parentContext = Propagator.Extract(
            default,
            message,
            static (article, key) => GetHeaderValue(article, key));
        var previousBaggage = Baggage.Current;
        Baggage.Current = parentContext.Baggage;

        using var activity = MonitoringExtensions.ActivitySource.StartActivity(
            "process newsletter article",
            ActivityKind.Consumer,
            parentContext.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.destination.name", SubscriptionId);
        activity?.SetTag("article.id", message.Id);

        try
        {
            logger.LogInformation(
                "Received article {ArticleId} for newsletter processing: {Title}.",
                message.Id,
                message.Title);

            // TOGGLE POINT 2: is the subscriber feature switched on? Evaluated for EVERY message.
            if (!toggles.IsEnabled(FeatureKeys.SubscriberService))
            {
                logger.LogInformation(
                    "Subscriber feature is OFF; skipping newsletter for article {ArticleId}.",
                    message.Id);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<ISubscriberClient>();
            var subscribers = await client.GetSubscribersAsync(cancellationToken);

            if (subscribers is null)
            {
                // DEGRADED MODE: the subscriber lane is unavailable. The failure stops here.
                logger.LogWarning(
                    "SubscriberService unavailable; newsletter for article {ArticleId} skipped (degraded mode).",
                    message.Id);
                return;
            }

            foreach (var subscriber in subscribers)
            {
                logger.LogInformation(
                    "Newsletter for article {ArticleId} sent to subscriber {SubscriberId}.",
                    message.Id,
                    subscriber.Id);
            }

            logger.LogInformation(
                "Newsletter for article {ArticleId} sent to {Count} subscribers.",
                message.Id,
                subscribers.Count);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogError(exception, "Failed to process article {ArticleId} for newsletters.", message.Id);
            throw;
        }
        finally
        {
            Baggage.Current = previousBaggage;
        }
    }

    private static IEnumerable<string>? GetHeaderValue(ArticlePublished article, string key)
    {
        if (!article.Header.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            string text => new[] { text },
            JsonElement element when element.ValueKind == JsonValueKind.String
                                     && element.GetString() is { } text => new[] { text },
            _ => null
        };
    }
}
