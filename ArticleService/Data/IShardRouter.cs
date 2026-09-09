using ArticleService.Models;

namespace ArticleService.Data;

/// <summary>
/// Resolves a Continent into the connection string of the physical
/// database that owns it. This interface is the single seam where the
/// z-axis split (data partitioning) lives - everything else in the
/// service just calls through it and doesn't need to know how many
/// databases exist or where they run.
/// </summary>
public interface IShardRouter
{
    string GetConnectionString(Continent continent);
}
