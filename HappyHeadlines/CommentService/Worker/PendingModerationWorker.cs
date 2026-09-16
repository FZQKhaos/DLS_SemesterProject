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
        var intervalSeconds = configuration.GetValue("PendingModeration:IntervalSeconds", 10);
        var batchSize = configuration.GetValue("PendingModeration:BatchSize", 20);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ModeratePendingBatch(batchSize, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
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

    private async Task ModeratePendingBatch(int batchSize, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICommentRepository>();
        var profanityClient = scope.ServiceProvider.GetRequiredService<IProfanityClient>();
        var pending = await repository.GetPendingAsync(batchSize, cancellationToken);

        foreach (var comment in pending)
        {
            var result = await profanityClient.FilterAsync(comment.OriginalBody, cancellationToken);
            if (!result.ServiceAvailable)
            {
                logger.LogInformation("ProfanityService still unavailable; {Count} pending comments remain deferred.", pending.Count);
                break;
            }

            await repository.MarkPublishedAsync(comment.Id, result.FilteredText, cancellationToken);
            logger.LogInformation("Comment {CommentId} moved from pending to published.", comment.Id);
        }
    }
}
