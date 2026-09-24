using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace Monitoring;

public static class MonitoringExtensions
{
    public static readonly ActivitySource ActivitySource =
        new("HappyHeadlines.Monitoring");
    
    public static WebApplicationBuilder AddMonitoring(
        this WebApplicationBuilder builder, 
        string serviceName)
    {
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource(ActivitySource.Name).SetSampler(new AlwaysOnSampler());
                tracing.AddConsoleExporter();
                tracing.AddZipkinExporter(options =>
                {
                    options.Endpoint = new Uri(
                        "http://zipkin:9411/api/v2/spans");
                });
            });
        
        builder.Services.AddSerilog((_, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override(
                    "Microsoft.AspNetCore",
                    LogEventLevel.Warning)
                .MinimumLevel.Override(
                    "Microsoft.Hosting.Lifetime",
                    LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithProperty("Service", serviceName)
                .Enrich.WithProperty(
                    "Environment",
                    builder.Environment.EnvironmentName)
                .WriteTo.Console()
                .WriteTo.Seq("http://seq:5341");
                
        });
        
        return builder;
    }
    
}