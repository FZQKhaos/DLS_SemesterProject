using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace Monitoring;

/// <summary>
/// All Happy Headlines services send structured logs, OTLP traces and metrics to Seq.
/// Console output is kept as a local emergency diagnostics fallback only.
/// </summary>
public static class MonitoringExtensions
{
    // Backward-compatible activity source for existing RabbitMQ publisher/subscribers.
    public static ActivitySource ActivitySource => Telemetry.Activities;

    public static WebApplicationBuilder AddMonitoring(this WebApplicationBuilder builder, string serviceName)
    {
        Configure(builder.Services, builder.Configuration, builder.Environment.EnvironmentName, serviceName);
        return builder;
    }

    public static HostApplicationBuilder AddMonitoring(this HostApplicationBuilder builder, string serviceName)
    {
        Configure(builder.Services, builder.Configuration, builder.Environment.EnvironmentName, serviceName);
        return builder;
    }

    private static void Configure(
        IServiceCollection services, IConfiguration configuration, string environment, string serviceName)
    {
        var seqUrl = (configuration["Observability:SeqUrl"] ?? "http://seq:5341").TrimEnd('/');
        var configuredKey = configuration["Observability:SeqApiKey"];
        var seqApiKey = string.IsNullOrWhiteSpace(configuredKey) ? null : configuredKey;

        // Serilog is the only logging provider; do not also export ILogger events via OTLP.
        services.AddSerilog((_, log) => log
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override(
                "System.Net.Http.HttpClient",
                LogEventLevel.Warning)
            .MinimumLevel.Override(
                "Microsoft.Extensions.Http",
                LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console()
            .WriteTo.Seq(seqUrl, apiKey: seqApiKey));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddSource(Telemetry.Activities.Name)
                // All requests for a small, local teaching demo; reduce in production.
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(1.0)))
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(seqUrl + "/ingest/otlp/v1/traces");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    if (!string.IsNullOrWhiteSpace(seqApiKey))
                        options.Headers = "X-Seq-ApiKey=" + seqApiKey;
                }))
            .WithMetrics(metrics => metrics
                .AddMeter(Telemetry.Meter.Name)
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("System.Runtime")
                .AddOtlpExporter((options, reader) =>
                {
                    options.Endpoint = new Uri(seqUrl + "/ingest/otlp/v1/metrics");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    if (!string.IsNullOrWhiteSpace(seqApiKey))
                        options.Headers = "X-Seq-ApiKey=" + seqApiKey;
                    // Seq requires delta temporality for ingested metrics.
                    reader.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                    reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10_000;
                }));
    }

    /// <summary>
    /// One structured completion event per request; excludes URLs, query strings, bodies,
    /// credentials and text. Health polling is ignored to avoid noisy dashboards.
    /// </summary>
    public static WebApplication UseHappyHeadlinesRequestLogging(this WebApplication app)
    {
        app.UseRouting();
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("HappyHeadlines.Http");

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await next();
                return;
            }

            var start = Stopwatch.GetTimestamp();
            try
            {
                await next();
                var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "<unmatched>";

                if (context.Response.StatusCode >= 500)
                    logger.LogError("HTTP {Method} {Route} returned {StatusCode} in {ElapsedMs} ms",
                        context.Request.Method, route, context.Response.StatusCode, elapsed);
                else
                    logger.LogInformation("HTTP {Method} {Route} returned {StatusCode} in {ElapsedMs} ms",
                        context.Request.Method, route, context.Response.StatusCode, elapsed);
            }
            catch (Exception exception)
            {
                var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "<unmatched>";
                logger.LogError(exception, "HTTP {Method} {Route} failed after {ElapsedMs} ms",
                    context.Request.Method, route, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                throw;
            }
        });
        return app;
    }
}
