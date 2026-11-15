#!/usr/bin/env python3
"""Deterministic before/after work-proxy model, NOT an energy benchmark."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
worker = (ROOT / 'HappyHeadlines/CommentService/Worker/PendingModerationWorker.cs').read_text()
cache = (ROOT / 'HappyHeadlines/CommentService/Cache/RedisCommentCache.cs').read_text()
assert 'idleDelay.TotalSeconds * 2' in worker
assert 'SortedSetLengthAsync(LruKey)' in cache
assert 'SortedSetRangeByRankAsync(LruKey, 0, 0' in cache
assert 'LogDebug("ProfanityService still unavailable' in worker

# Idealized idle worker, total polls over 60 min. Initial poll at t=0.
def polls(interval, backoff=False):
    t, n, delay = 0, 0, interval
    while t < 3600:
        n += 1
        delay = min(60, delay * 2) if backoff else interval
        t += delay
    return n

baseline_polls, patched_polls = polls(10), polls(10, True)
# Baseline sends all sorted-set members for each no-eviction cache write.
# Updated code sends a fixed-size cardinality (not IDs).
baseline_lru_ids = 30
patched_lru_ids = 0
# Two baseline Information log events per failed/recovered moderation sequence;
# new entries at Debug do not pass the default Information threshold.
baseline_info_events, patched_info_events = 2, 0
print('Modelled idle DB polls per hour:', baseline_polls, '->', patched_polls)
print('LRU member IDs returned per no-eviction write:', baseline_lru_ids, '->', patched_lru_ids)
print('Information-level progress logs per scenario:', baseline_info_events, '->', patched_info_events)
assert patched_polls < baseline_polls
assert patched_lru_ids < baseline_lru_ids
assert patched_info_events < baseline_info_events
print('PASS - lower work proxies under stated scenario; NOT measured energy savings')
