using Monitoring;
using CommentService.Cache;
using CommentService.Client.Interface;
using CommentService.Data.Interface;

namespace CommentService.Worker;

public sealed class PendingModerationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PendingModerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(1, configuration.GetValue("PendingModeration:IntervalSeconds", 10));
        var batchSize = configuration.GetValue("PendingModeration:BatchSize", 20);
        var idleDelay = TimeSpan.FromSeconds(intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // GAF: Back off polling when nothing is waiting for moderation.
                var hadPending = await ModeratePendingBatch(batchSize, stoppingToken);
                idleDelay = hadPending
                    ? TimeSpan.FromSeconds(intervalSeconds)
                    : TimeSpan.FromSeconds(Math.Min(60, idleDelay.TotalSeconds * 2));
                await Task.Delay(idleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pending moderation cycle failed.");
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }

    private async Task<bool> ModeratePendingBatch(int batchSize, CancellationToken cancellationToken)
    {
        using var activity = MonitoringExtensions.ActivitySource.StartActivity("comment.pending_moderation.batch");
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICommentRepository>();
        var profanityClient = scope.ServiceProvider.GetRequiredService<IProfanityClient>();
        var commentCache = scope.ServiceProvider.GetRequiredService<ICommentCache>();
        var pending = await repository.GetPendingAsync(batchSize, cancellationToken);
        if (pending.Count == 0) return false;

        foreach (var comment in pending)
        {
            var result = await profanityClient.FilterAsync(comment.OriginalBody, cancellationToken);
            if (!result.ServiceAvailable)
            {
                logger.LogDebug("ProfanityService still unavailable; {Count} pending comments remain deferred.", pending.Count);
                break;
            }

            await repository.MarkPublishedAsync(comment.Id, result.FilteredText, cancellationToken);
            // Publishing changes what should appear in article comment lists. Removing
            // the cached entry prevents Redis from serving the older list where this
            // comment was still pending. The next request rebuilds the cache from the
            // database instead of trying to patch the JSON payload in place.
            await commentCache.RemoveCommentsAsync(comment.ArticleId, cancellationToken);
            Telemetry.RecordWorkflow("comment.moderation", "published");
            logger.LogDebug("Comment {CommentId} moved from pending to published.", comment.Id);
        }
        return true;
    }
}


// Potential to do here, too much logs spam every 10 sec. but major rework needs to be done to avoid it while keeping active monitoring.
// - Johan