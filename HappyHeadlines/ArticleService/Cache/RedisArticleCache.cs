using System.Text.Json;
using ArticleService.Domain;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ArticleService.Cache;

public sealed class RedisArticleCache(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisArticleCache> logger) : IArticleCache
{
    private const string HitsKey = "metrics:article-cache:hits";
    private const string MissesKey = "metrics:article-cache:misses";
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    // This cache stores only global articles. The key includes "global" so the Redis
    // namespace communicates the architectural boundary and cannot be confused with
    // regional article data.
    public async Task<Article?> GetArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(id);
        var value = await database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            // A cache miss means Redis answered successfully, but the requested article
            // was absent. That is useful metric data because it helps calculate whether
            // the offline worker is caching the data users actually request.
            await IncrementMetricAsync(MissesKey);
            return null;
        }

        try
        {
            byte[]? payload = value;
            if (payload is null)
            {
                // Treat an unreadable empty payload as a miss rather than a hit. The
                // service can still fall back to the database, and the hit ratio remains
                // about successfully served cache lookups.
                await IncrementMetricAsync(MissesKey);
                return null;
            }

            // A hit means the requested article was found in Redis and can be returned
            // without reading the geographically remote Global ArticleDatabase.
            await IncrementMetricAsync(HitsKey);
            return JsonSerializer.Deserialize<Article>(payload);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Failed to deserialize article from Redis key {RedisKey}.", key);
            throw;
        }
    }

    public async Task SetArticleAsync(string id, Article article, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (expiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration must be greater than zero.");

        var key = BuildKey(id);
        var payload = JsonSerializer.SerializeToUtf8Bytes(article);

        try
        {
            // Writes normally come from ArticleCacheWorker, not from ArticleService
            // request handling. That makes ArticleCache proactive/offline rather than
            // reactive/cache-aside.
            await database.StringSetAsync(key, payload, expiration);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to write article {ArticleId} to Redis key {RedisKey}.", article.Id, key);
            throw;
        }
    }

    private async Task IncrementMetricAsync(string metricKey)
    {
        try
        {
            // Hits and misses make the cache observable:
            // hit ratio = hits / (hits + misses) * 100.
            // Regional article requests should not affect these counters because they
            // bypass ArticleCache by design.
            await database.StringIncrementAsync(metricKey);
        }
        catch (RedisException exception)
        {
            // Metric writes should not make the service fail. Redis is useful for
            // observability here, but the article read path has its own fallback behavior.
            logger.LogWarning(exception, "Redis cache metric increment failed for {MetricKey}.", metricKey);
        }
    }

    private static string BuildKey(string id) => $"article:global:{id}";
}
