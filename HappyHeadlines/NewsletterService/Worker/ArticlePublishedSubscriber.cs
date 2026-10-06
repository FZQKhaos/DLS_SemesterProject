using System.Diagnostics;
using System.Text.Json;
using EasyNetQ;
using Events;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace NewsletterService.Worker;

public sealed class ArticlePublishedSubscriber(
    IBus bus,
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

    private Task HandleMessageAsync(ArticlePublished message, CancellationToken cancellationToken)
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
            return Task.CompletedTask;
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
