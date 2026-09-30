using System.Diagnostics;
using System.Text.Json;
using ArticleService.Data.Interface;
using ArticleService.Domain;
using EasyNetQ;
using Events;
using Microsoft.Extensions.DependencyInjection;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace ArticleService.Worker;

public sealed class ArticlePublishedSubscriber(
    IBus bus,
    IServiceScopeFactory scopeFactory,
    ILogger<ArticlePublishedSubscriber> logger) : IHostedService
{
    private const string SubscriptionId = "ArticleService";
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
            "process published article",
            ActivityKind.Consumer,
            parentContext.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.destination.name", SubscriptionId);
        activity?.SetTag("article.id", message.Id);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
            await repository.SavePublishedArticle(
                new Article
                {
                    Id = message.Id,
                    Title = message.Title,
                    Body = message.Body,
                    Continent = message.Continent,
                    PublishedAtUtc = message.PublishedAtUtc
                },
                cancellationToken);

            logger.LogInformation("Stored published article {ArticleId}.", message.Id);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogError(exception, "Failed to store published article {ArticleId}.", message.Id);
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
