using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
    var options = ConfigurationOptions.Parse(connectionString);
    // The dashboard reads metrics from Redis, the shared place where both services
    // increment cache hit/miss counters.
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 5000;
    options.SyncTimeout = 5000;
    return ConnectionMultiplexer.Connect(options);
});

var app = builder.Build();

app.MapGet("/", async (IConnectionMultiplexer connectionMultiplexer) =>
{
    // The dashboard intentionally reports simple cache effectiveness metrics only.
    // It does not introduce new monitoring infrastructure; it reads the counters that
    // the cache implementations already maintain.
    var articleStats = await GetStatsAsync(connectionMultiplexer, "metrics:article-cache:hits", "metrics:article-cache:misses");
    var commentStats = await GetStatsAsync(connectionMultiplexer, "metrics:comment-cache:hits", "metrics:comment-cache:misses");

    var html = "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\" /><title>Cache Dashboard</title>" +
        "<style>body { font-family: Arial, sans-serif; margin: 2rem; background: #f5f5f5; }" +
        ".panel { background: white; border-radius: 8px; padding: 1.25rem; margin-bottom: 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.12); max-width: 700px; }" +
        "h2 { margin-top: 0; } table { border-collapse: collapse; width: 100%; } th, td { border-bottom: 1px solid #ddd; padding: 0.5rem 0.75rem; text-align: left; } th { width: 55%; }</style></head><body>" +
        "<h1>Cache Dashboard</h1><div class=\"panel\"><h2>ArticleCache</h2><table>" +
        $"<tr><th>Hits</th><td>{articleStats.Hits}</td></tr>" +
        $"<tr><th>Misses</th><td>{articleStats.Misses}</td></tr>" +
        $"<tr><th>Total lookups</th><td>{articleStats.Total}</td></tr>" +
        $"<tr><th>Hit ratio</th><td>{articleStats.HitRatio:0.##}%</td></tr>" +
        "</table></div><div class=\"panel\"><h2>CommentCache</h2><table>" +
        $"<tr><th>Hits</th><td>{commentStats.Hits}</td></tr>" +
        $"<tr><th>Misses</th><td>{commentStats.Misses}</td></tr>" +
        $"<tr><th>Total lookups</th><td>{commentStats.Total}</td></tr>" +
        $"<tr><th>Hit ratio</th><td>{commentStats.HitRatio:0.##}%</td></tr>" +
        "</table></div></body></html>";

    return Results.Content(html, "text/html");
});

app.MapGet("/api/cache-stats", async (IConnectionMultiplexer connectionMultiplexer) =>
{
    var articleStats = await GetStatsAsync(connectionMultiplexer, "metrics:article-cache:hits", "metrics:article-cache:misses");
    var commentStats = await GetStatsAsync(connectionMultiplexer, "metrics:comment-cache:hits", "metrics:comment-cache:misses");

    return Results.Json(new
    {
        articleCache = new
        {
            hits = articleStats.Hits,
            misses = articleStats.Misses,
            total = articleStats.Total,
            hitRatio = articleStats.HitRatio
        },
        commentCache = new
        {
            hits = commentStats.Hits,
            misses = commentStats.Misses,
            total = commentStats.Total,
            hitRatio = commentStats.HitRatio
        }
    });
});

app.Run();

static async Task<CacheStats> GetStatsAsync(IConnectionMultiplexer connectionMultiplexer, string hitsKey, string missesKey)
{
    var database = connectionMultiplexer.GetDatabase();
    var hits = await GetLongAsync(database, hitsKey);
    var misses = await GetLongAsync(database, missesKey);
    var total = hits + misses;
    // Cache hit ratio:
    // Hits are requests served from cache. Misses are successful cache lookups where
    // the data was absent. HitRatio = Hits / (Hits + Misses) * 100.
    //
    // If there have been no lookups, the ratio is reported as 0 to avoid division by
    // zero. A high ratio usually means lower database load, lower latency, and better
    // tolerance of backing database slowness, but it is not a complete performance
    // measure. Trade-offs still include Redis memory use, stale data, invalidation
    // complexity, and data freshness.
    var hitRatio = total == 0 ? 0d : (double)hits / total * 100d;
    return new CacheStats(hits, misses, total, hitRatio);
}

static async Task<long> GetLongAsync(IDatabase database, string key)
{
    var value = await database.StringGetAsync(key);
    return value.IsNull ? 0 : (long)value;
}

public sealed record CacheStats(long Hits, long Misses, long Total, double HitRatio);
