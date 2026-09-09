namespace ArticleService.Models;

/// <summary>
/// The eight article databases from the z-axis split: one per continent,
/// plus <see cref="Global"/> for articles relevant to the entire world.
/// This enum value is what the shard router (see Data/ShardRouter.cs)
/// uses to pick which physical database to talk to.
/// </summary>
public enum Continent
{
    Africa,
    Antarctica,
    Asia,
    Europe,
    NorthAmerica,
    SouthAmerica,
    Oceania,
    Global
}
