using FeatureHubSDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FeatureToggles;

/// <summary>
/// FeatureHub implementation of IFeatureToggles.
/// Also a BackgroundService, so the connection to FeatureHub is built in the background
/// and NEVER blocks or breaks application startup (fault isolation).
///
/// Safe default: if FeatureHub is not reached yet, or the key is missing, every flag is OFF.
/// </summary>
public sealed class FeatureHubToggleService(
    IConfiguration configuration,
    ILogger<FeatureHubToggleService> logger) : BackgroundService, IFeatureToggles
{
    // volatile: request threads always see the latest value written by the background thread.
    private volatile IClientContext? context;
    private IFeatureHubConfig? config;

    public bool IsEnabled(string featureKey)
    {
        var current = context;
        if (current is null)
        {
            return false; // not connected yet -> safe default: OFF
        }

        try
        {
            // In-memory lookup; FeatureHub keeps the value up to date through streaming.
            return current[featureKey].IsEnabled;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not evaluate feature {FeatureKey}; treating it as OFF.", featureKey);
            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var edgeUrl = configuration["FeatureHub:EdgeUrl"];
        var apiKey = configuration["FeatureHub:ApiKey"];

        if (string.IsNullOrWhiteSpace(edgeUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("FeatureHub:EdgeUrl or FeatureHub:ApiKey is missing. All features stay OFF.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                config = new EdgeFeatureHubConfig(edgeUrl, apiKey);
                await config.Init();
                context = await config.NewContext().Build();

                logger.LogInformation("Connected to FeatureHub at {EdgeUrl}.", edgeUrl);
                return; // the SDK keeps the streaming connection alive on its own
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "FeatureHub is not reachable yet. Retrying in 10 seconds. Features stay OFF.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // application is shutting down
                }
            }
        }
    }
}
