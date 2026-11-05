namespace FeatureToggles;

/// <summary>
/// Toggle router: the place where code asks whether a feature is switched on.
/// Always evaluate at the point of use, never cache the result in a variable.
/// </summary>
public interface IFeatureToggles
{
    bool IsEnabled(string featureKey);
}
