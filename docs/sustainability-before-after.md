# Sustainability changes - BEFORE / AFTER / measurement protocol

These are **three new tactics**, not the existing cache proximity, bulk global
article fetch, or Npgsql connection pooling. Tactic source: Henrik Bærbak
Christensen, [Green Architecture Framework](https://baerbak.cs.au.dk/papers/gaf.html).

## 1. Shut Down When Idle: adaptive pending moderation polling

**Before** `CommentService/Worker/PendingModerationWorker.cs`:

```csharp
await ModeratePendingBatch(batchSize, stoppingToken);
await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
```

**After**:

```csharp
var hadPending = await ModeratePendingBatch(batchSize, stoppingToken);
idleDelay = hadPending
    ? TimeSpan.FromSeconds(intervalSeconds)
    : TimeSpan.FromSeconds(Math.Min(60, idleDelay.TotalSeconds * 2));
await Task.Delay(idleDelay, stoppingToken);
```

With no pending comments, database polling backs off 10 => 20 => 40 => 60 s.
When pending work is found, the original 10-second interval is restored. This
reduces idle database reads (at the tradeoff of longer reaction latency to new
pending comments during an extended idle period).

## 2. Avoid Unnecessary Resources / Reduce Network Package Size: Redis LRU

**Before** `CommentService/Cache/RedisCommentCache.cs`:

```csharp
var members = await database.SortedSetRangeByRankAsync(LruKey, 0, -1, Order.Ascending);
while (members.Length > MaxArticles)
{
    var oldest = members[0];
    // delete oldest
    members = await database.SortedSetRangeByRankAsync(LruKey, 0, -1, Order.Ascending);
}
```

**After**:

```csharp
var count = await database.SortedSetLengthAsync(LruKey);
while (count > MaxArticles)
{
    var oldest = await database.SortedSetRangeByRankAsync(LruKey, 0, 0, Order.Ascending);
    if (oldest.Length == 0) break;
    await database.SortedSetRemoveAsync(LruKey, oldest[0]);
    await database.KeyDeleteAsync(BuildArticleKey(oldest[0].ToString()));
    count--;
}
```

Formerly each LRU insertion returned **all** cached IDs (~31 entries at cap+1);
now a scalar count, and (only on overflow) a **single** oldest ID. This avoids
unnecessary bytes returned by Redis on the ordinary no-eviction path.

## 3. Accept Lower Fidelity / Reduce Logging: avoid repeated low-value logs

**Before** `CommentService/Worker/PendingModerationWorker.cs`:

```csharp
logger.LogInformation("ProfanityService still unavailable; ...", ...);
logger.LogInformation("Comment {CommentId} moved from pending to published.", ...);
```

**After**:

```csharp
logger.LogDebug("ProfanityService still unavailable; ...", ...);
logger.LogDebug("Comment {CommentId} moved from pending to published.", ...);
```

This preserves exceptions and errors but excludes the high-frequency progression
messages at the existing default Information threshold. Tradeoff: fewer details
in production logs. Change logging levels when diagnosing incidents.

## Verification, NOT fabricated energy measurements

`python3 scripts/verify-gaf.py` checks deterministic work proxies from the exact
old/new algorithms: moderation DB polls in a 1h idle period, LRU member payload
per insert at 30-item capacity, and emitted Information logs for a fixed simulated
batch. It confirms code-level reductions, *not* watts or joules.

For verified **energy** improvements, follow GAF's Measure and Experiment process:
1. Same host/hardware, same workload, no other major processes; disable autoscaling.
2. On baseline branch, record `docker stats`, requests completed, latency, and
   RAPL `energy_uj` on a supported Linux CPU (or an external power meter).
3. Restore exactly the same databases and queue state and repeat the same workload
   on patched code, e.g. 5 trials of 60 minutes idle moderation, 10k cache LRU
   accesses, 1000 moderation failures and recoveries.
4. Compare **joules per successful transaction** and 95% confidence intervals;
   explicitly record total energy/elapsed time and test correctness. If RAPL or
   wall-meter readings are unavailable, report work proxies only.

No measured joules are provided here because no Docker runtime / power meter is
available in the authoring environment. Do not use the proxy results as measured
energy savings.
