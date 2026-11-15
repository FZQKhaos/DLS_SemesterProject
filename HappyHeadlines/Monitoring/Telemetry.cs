using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Monitoring;

/// <summary>Consistent, low-cardinality telemetry shared by all Happy Headlines processes.</summary>
public static class Telemetry
{
    public static readonly ActivitySource Activities = new("HappyHeadlines");
    public static readonly Meter Meter = new("HappyHeadlines");

    private static readonly Counter<long> CacheLookups =
        Meter.CreateCounter<long>("happyheadlines.cache.lookups", "{lookup}",
            "Cache lookups by cache name and outcome");

    private static readonly Counter<long> WorkflowEvents =
        Meter.CreateCounter<long>("happyheadlines.workflow.events", "{event}",
            "Business workflow transitions");

    public static void RecordCacheLookup(string cache, string result) =>
        CacheLookups.Add(1,
            new KeyValuePair<string, object?>("cache.name", cache),
            new KeyValuePair<string, object?>("result", result));

    public static void RecordWorkflow(string workflow, string result) =>
        WorkflowEvents.Add(1,
            new KeyValuePair<string, object?>("workflow", workflow),
            new KeyValuePair<string, object?>("result", result));
}
