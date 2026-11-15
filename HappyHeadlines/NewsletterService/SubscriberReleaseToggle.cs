using StackExchange.Redis;

namespace NewsletterService;

public sealed class SubscriberReleaseToggle(
    IConnectionMultiplexer redis, ILogger<SubscriberReleaseToggle> logger)
{
    public const string Key = "feature:subscriber-service:enabled";

    // Check on every message, not only at startup; no redeployment or restart.
    public async Task<bool> IsEnabledAsync()
    {
        try { return await redis.GetDatabase().StringGetAsync(Key) == "1"; }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Subscriber toggle lookup failed; feature disabled");
            return false;
        }
    }

    public Task SetAsync(bool enabled) =>
        redis.GetDatabase().StringSetAsync(Key, enabled ? "1" : "0");
}
