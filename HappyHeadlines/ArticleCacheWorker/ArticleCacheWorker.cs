using ArticleService.Cache;
using ArticleService.Data.Interface;
using ArticleService.Domain;

public sealed class ArticleCacheWorker(
    IArticleRepository articleRepository,
    IArticleCache articleCache,
    ILogger<ArticleCacheWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    private const int DefaultRetentionDays = 14;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refreshIntervalSeconds = configuration.GetValue<int?>("ArticleCache:RefreshIntervalSeconds") ?? 300;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(refreshIntervalSeconds));

        try
        {
            await RefreshCacheAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshCacheAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Article cache worker is stopping.");
        }
    }

    private async Task RefreshCacheAsync(CancellationToken stoppingToken)
    {
        try
        {
            var cutoffUtc = DateTime.UtcNow.AddDays(-DefaultRetentionDays);
            var globalArticles = await articleRepository.GetArticlesPublishedSince("global", cutoffUtc, stoppingToken);

            var writtenCount = 0;
            foreach (var article in globalArticles)
            {
                var expiration = article.PublishedAtUtc.AddDays(DefaultRetentionDays) - DateTime.UtcNow;
                if (expiration <= TimeSpan.Zero)
                {
                    logger.LogInformation(
                        "Skipping article {ArticleId} because its cache expiration is {Expiration}.",
                        article.Id,
                        expiration);
                    continue;
                }

                await articleCache.SetArticleAsync(article.Id, article, expiration, stoppingToken);
                writtenCount++;
            }

            logger.LogInformation("Loaded {ArticleCount} global articles into the article cache.", writtenCount);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to refresh the global article cache.");
        }
    }
}
