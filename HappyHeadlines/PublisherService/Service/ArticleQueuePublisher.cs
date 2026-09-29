using System.Diagnostics;
using EasyNetQ;
using Events;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using PublisherService.Domain;

namespace PublisherService.Service;

public sealed class ArticleQueuePublisher(IBus bus)
{
    private static readonly TraceContextPropagator Propagator = new();

    public async Task PublishAsync(Article article)
    {
        using var activity = MonitoringExtensions.ActivitySource.StartActivity(
            "publish article",
            ActivityKind.Producer);
        var message = new ArticlePublished
        {
            Id = article.Id,
            Title = article.Title,
            Body = article.Body,
            Continent = article.Continent
        };
        Propagator.Inject(
            new PropagationContext(activity?.Context ?? Activity.Current?.Context ?? default, Baggage.Current),
            message,
            static (articleMessage, key, value) => articleMessage.Header[key] = value);
        activity?.SetTag("article.id", article.Id);
        await bus.PubSub.PublishAsync(message);
    }
}
