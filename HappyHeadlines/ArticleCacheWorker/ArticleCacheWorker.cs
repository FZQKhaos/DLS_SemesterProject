using ArticleService.Cache;
using ArticleService.Data.Interface;
using ArticleService.Domain;

public sealed class ArticleCacheWorker(
    IArticleRepository articleRepository,
    IArticleCache articleCache,
    ILogger<ArticleCacheWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    // Week 40 ArticleCache rule:
    // Only global articles from the latest 14 days are kept in Redis. The global
    // article database is the shared, geographically remote bottleneck, so this
    // worker reduces repeated reads against that database before users ask for data.
    private const int DefaultRetentionDays = 14;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refreshIntervalSeconds = configuration.GetValue<int?>("ArticleCache:RefreshIntervalSeconds") ?? 300;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(refreshIntervalSeconds));

        try
        {
            // ArticleCache is populated proactively by this background/offline process.
            // This is deliberately different from cache-aside: user requests read the
            // cache, but they do not fill it on a miss. Cache freshness therefore depends
            // on this worker's refresh interval, while request handling stays simple.
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
            // The worker scans the global article source for the retention window and
            // refreshes Redis with recent global articles. Keeping these close to the
            // application can improve latency, reduce database load, and keep reads
            // available for cached articles if the global database is temporarily slow.
            var cutoffUtc = DateTime.UtcNow.AddDays(-DefaultRetentionDays);
            var globalArticles = await articleRepository.GetArticlesPublishedSince("global", cutoffUtc, stoppingToken);

            var writtenCount = 0;
            foreach (var article in globalArticles)
            {
                // Expiration is anchored to the article's publication time, not to the
                // time this worker happens to run. An article that is already 10 days old
                // should stay cached for about 4 more days, not receive a fresh 14-day TTL
                // on every refresh cycle.
                var expiration = article.PublishedAtUtc.AddDays(DefaultRetentionDays) - DateTime.UtcNow;
                if (expiration <= TimeSpan.Zero)
                {
                    logger.LogInformation(
                        "Skipping article {ArticleId} because its cache expiration is {Expiration}.",
                        article.Id,
                        expiration);
                    continue;
                }

                // Trade-off: proactive caching means recent global articles may already be
                // fast to read, but Redis space can be used for articles that no one asks for.
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
