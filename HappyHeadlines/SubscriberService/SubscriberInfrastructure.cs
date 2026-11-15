using EasyNetQ;
using StackExchange.Redis;

namespace SubscriberService;

public sealed class SubscriberQueueBus(IBus bus) : IDisposable
{
    public IBus Bus { get; } = bus;
    public void Dispose() => Bus.Dispose();
}

public sealed class RuntimeSubscriberToggle(IConnectionMultiplexer redis, ILogger<RuntimeSubscriberToggle> logger)
{
    public const string Key = "feature:subscriber-service:enabled";

    public async Task<bool> IsEnabledAsync()
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(Key);
            return value == "1";
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Release toggle unavailable, SubscriberService disabled");
            return false;
        }
    }
}
