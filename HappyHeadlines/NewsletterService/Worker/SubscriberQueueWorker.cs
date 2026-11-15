using EasyNetQ;
using System.Net;
using System.Net.Mail;
using Events;
using NewsletterService;

namespace NewsletterService.Worker;

// Only this component reads subscriber notifications. The ArticlePublished worker
// remains independent if SubscriberService or its broker fails.
public sealed class SubscriberQueueWorker(
    SubscriberQueueBus subscriberBus, SubscriberReleaseToggle toggle,
    IConfiguration config, ILogger<SubscriberQueueWorker> logger) : BackgroundService
{
    private IDisposable? subscription;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                subscription = await subscriberBus.Bus.PubSub.SubscribeAsync<SubscriberNotification>(
                    "SubscriberQueue", HandleAsync, _ => { }, cancellationToken: stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Subscriber queue unreachable; article processing continues");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally { subscription?.Dispose(); }
        }
    }

    private async Task HandleAsync(SubscriberNotification message, CancellationToken ct)
    {
        if (!await toggle.IsEnabledAsync())
        {
            // Release toggle drops subscriber feature work; core news flow is unaffected.
            logger.LogDebug("Subscriber feature disabled, skipped {NotificationId}", message.NotificationId);
            return;
        }

        var host = config["Smtp:Host"];
        var from = config["Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            logger.LogWarning("SMTP is not configured: notification {NotificationId} was NOT emailed", message.NotificationId);
            return;
        }

        using var smtp = new SmtpClient(host, config.GetValue("Smtp:Port", 587))
        {
            EnableSsl = config.GetValue("Smtp:EnableSsl", true)
        };
        var username = config["Smtp:Username"];
        if (!string.IsNullOrEmpty(username))
            smtp.Credentials = new NetworkCredential(username, config["Smtp:Password"]);
        using var mail = new MailMessage(from, message.Email, message.Subject, message.Body);
        await smtp.SendMailAsync(mail, ct);
        logger.LogInformation("Emailed subscriber notification {NotificationId} ({Kind})", message.NotificationId, message.Kind);
    }
}

public sealed class SubscriberQueueBus(IBus bus) : IDisposable
{
    public IBus Bus { get; } = bus;
    public void Dispose() => Bus.Dispose();
}
