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

    public async Task<Article?> GetArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(id);
        var value = await database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            await IncrementMetricAsync(MissesKey);
            return null;
        }

        try
        {
            byte[]? payload = value;
            if (payload is null)
            {
                await IncrementMetricAsync(MissesKey);
                return null;
            }

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
            await database.StringIncrementAsync(metricKey);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis cache metric increment failed for {MetricKey}.", metricKey);
        }
    }

    private static string BuildKey(string id) => $"article:global:{id}";
}
