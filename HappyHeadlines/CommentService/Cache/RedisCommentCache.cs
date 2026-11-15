using Monitoring;
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
    // The Week 40 limit is 30 article IDs, not 30 individual comments. Each cached
    // Redis payload may contain many comments for one article.
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
                // A miss means Redis was reached but no cached payload exists for this
                // article. Redis outages are handled separately below and should not be
                // counted as ordinary cache misses.
                Telemetry.RecordCacheLookup("comment", "miss");
                return null;
            }

            // A hit means Redis contains the comment payload, including the valid case
            // where that payload is an empty JSON list: [].
            Telemetry.RecordCacheLookup("comment", "hit");
            // Accessing an article updates its recency. This is what makes the eviction
            // policy least-recently-used rather than simply oldest article by time.
            await TouchLruAsync(articleId, cancellationToken);
            var comments = JsonSerializer.Deserialize<List<Comment>>(payload!, JsonOptions);
            // An empty collection is a real cached result. It prevents articles with zero
            // comments from repeatedly hitting CommentDatabase just to rediscover that
            // there are still no comments.
            return comments ?? new List<Comment>();
        }
        catch (RedisException exception)
        {
            // Redis is an optimization layer. If it is unavailable, the service can fall
            // back to CommentDatabase via the null return value instead of making comment
            // reads unusable solely because the cache infrastructure failed.
            Telemetry.RecordCacheLookup("comment", "unavailable");
            logger.LogError(exception, "Redis comment cache read failed for article {ArticleId}; falling back to database.", articleId);
            return null;
        }
        catch (JsonException exception)
        {
            // A corrupt cached payload is treated like an unusable cache entry. Returning
            // null lets the caller reload from the database and continue serving users.
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
            // Cache-aside write: after CommentService loads the complete comment list from
            // the database, Redis stores that list for the next request. Empty lists are
            // serialized too because "no comments" is valid data, not a miss.
            var payload = JsonSerializer.SerializeToUtf8Bytes(comments ?? Array.Empty<Comment>());
            await database.StringSetAsync(key, payload);
            await TouchLruAsync(articleId, cancellationToken);
            await EnforceLruLimitAsync();
        }
        catch (RedisException exception)
        {
            // A failed cache write is logged but not rethrown. The database operation
            // already succeeded, so Redis should improve latency/load without becoming
            // an unnecessary dependency for the main comment read path.
            logger.LogError(exception, "Failed to write comment cache for article {ArticleId}.", articleId);
        }
    }

    public async Task RemoveCommentsAsync(string articleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildArticleKey(articleId);
        try
        {
            // Invalidation removes both the cached comment payload and its LRU membership.
            // This keeps the capacity index consistent with the actual cached entries.
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
        // Redis Sorted Set is a good fit for LRU:
        // member = articleId, score = last access time.
        // Updating the score on every hit/write turns recency into queryable state.
        await database.SortedSetAddAsync(LruKey, articleId, score);
    }

    private async Task EnforceLruLimitAsync()
    {
        // GAF: Avoid Unnecessary Resources. Fetch a scalar count, not a whole
        // list of up to 31 Redis members on every cache insertion.
        var count = await database.SortedSetLengthAsync(LruKey);
        while (count > MaxArticles)
        {
            // Only fetch the least recently used ID when eviction is necessary.
            var oldest = await database.SortedSetRangeByRankAsync(LruKey, 0, 0, Order.Ascending);
            if (oldest.Length == 0) break;
            await database.SortedSetRemoveAsync(LruKey, oldest[0]);
            await database.KeyDeleteAsync(BuildArticleKey(oldest[0].ToString()));
            count--;
        }
    }

    private static string BuildArticleKey(string articleId) => $"comment:article:{articleId}";
}
