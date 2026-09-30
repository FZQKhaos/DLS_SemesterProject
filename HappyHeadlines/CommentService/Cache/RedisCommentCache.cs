using System.Text.Json;
using CommentService.Domain;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommentService.Cache;

public sealed class RedisCommentCache(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisCommentCache> logger) : ICommentCache
{
    private const string LruKey = "comment-cache:lru";
    private const int MaxArticles = 30;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task<IReadOnlyList<Comment>?> GetCommentsAsync(string articleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildArticleKey(articleId);

        try
        {
            var payload = await database.StringGetAsync(key);
            if (payload.IsNullOrEmpty)
            {
                await IncrementMetricAsync("metrics:comment-cache:misses");
                return null;
            }

            await IncrementMetricAsync("metrics:comment-cache:hits");
            await TouchLruAsync(articleId, cancellationToken);
            var comments = JsonSerializer.Deserialize<List<Comment>>(payload!, JsonOptions);
            return comments ?? new List<Comment>();
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Redis comment cache read failed for article {ArticleId}; falling back to database.", articleId);
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Failed to deserialize cached comments for article {ArticleId}.", articleId);
            return null;
        }
    }

    public async Task SetCommentsAsync(string articleId, IReadOnlyList<Comment> comments, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildArticleKey(articleId);
        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(comments ?? Array.Empty<Comment>());
            await database.StringSetAsync(key, payload);
            await TouchLruAsync(articleId, cancellationToken);
            await EnforceLruLimitAsync();
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to write comment cache for article {ArticleId}.", articleId);
        }
    }

    public async Task RemoveCommentsAsync(string articleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildArticleKey(articleId);
        try
        {
            await database.KeyDeleteAsync(key);
            await database.SortedSetRemoveAsync(LruKey, articleId);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to invalidate comment cache for article {ArticleId}.", articleId);
        }
    }

    public async Task<bool> IsArticleCachedAsync(string articleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildArticleKey(articleId);
        try
        {
            return await database.KeyExistsAsync(key);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to check comment cache membership for article {ArticleId}.", articleId);
            return false;
        }
    }

    private async Task TouchLruAsync(string articleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await database.SortedSetAddAsync(LruKey, articleId, score);
    }

    private async Task EnforceLruLimitAsync()
    {
        var members = await database.SortedSetRangeByRankAsync(LruKey, 0, -1, Order.Ascending);
        while (members.Length > MaxArticles)
        {
            var oldest = members[0];
            await database.SortedSetRemoveAsync(LruKey, oldest);
            await database.KeyDeleteAsync(BuildArticleKey(oldest.ToString()));
            members = await database.SortedSetRangeByRankAsync(LruKey, 0, -1, Order.Ascending);
        }
    }

    private async Task IncrementMetricAsync(string key)
    {
        try
        {
            await database.StringIncrementAsync(key);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis metric increment failed for {MetricKey}.", key);
        }
    }

    private static string BuildArticleKey(string articleId) => $"comment:article:{articleId}";
}
