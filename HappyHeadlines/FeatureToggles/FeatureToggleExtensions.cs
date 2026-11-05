using Microsoft.Extensions.DependencyInjection;

namespace FeatureToggles;

public static class FeatureToggleExtensions
{
    /// <summary>
    /// Registers the toggle service as ONE shared instance that is both injectable as
    /// IFeatureToggles and started as a hosted (background) service.
    /// </summary>
    public static IServiceCollection AddFeatureToggles(this IServiceCollection services)
    {
        services.AddSingleton<FeatureHubToggleService>();
        services.AddSingleton<IFeatureToggles>(sp => sp.GetRequiredService<FeatureHubToggleService>());
        services.AddHostedService(sp => sp.GetRequiredService<FeatureHubToggleService>());
        return services;
    }
}
