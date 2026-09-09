namespace ArticleService.Configuration;

public static class ArticleRegions
{
    public const string Africa = "africa";
    public const string Antarctica = "antarctica";
    public const string Asia = "asia";
    public const string Europe = "europe";
    public const string NorthAmerica = "north-america";
    public const string SouthAmerica = "south-america";
    public const string Oceania = "oceania";
    public const string Global = "global";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Africa,
        Antarctica,
        Asia,
        Europe,
        NorthAmerica,
        SouthAmerica,
        Oceania,
        Global
    };

    public static bool TryNormalize(string? region, out string normalized)
    {
        normalized = (region ?? string.Empty).Trim().ToLowerInvariant();
        return All.Contains(normalized, StringComparer.Ordinal);
    }

    public static string GetConfigurationName(string region) => region switch
    {
        Africa => "Africa",
        Antarctica => "Antarctica",
        Asia => "Asia",
        Europe => "Europe",
        NorthAmerica => "NorthAmerica",
        SouthAmerica => "SouthAmerica",
        Oceania => "Oceania",
        Global => "Global",
        _ => throw new ArgumentOutOfRangeException(nameof(region), region, "Unknown article region")
    };
}
