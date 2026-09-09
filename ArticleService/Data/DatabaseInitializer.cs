using ArticleService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Data;

/// <summary>
/// Creates the Articles table in all eight continent databases if it
/// doesn't already exist. This is meant to run ONCE, from a dedicated
/// docker-compose service, before any of the three ArticleService
/// replicas start accepting traffic - see the "database-initializer"
/// service in docker-compose.yml. If all three replicas tried to do
/// this themselves on every startup, they could race each other and
/// fail while SQL Server is still creating the schema.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAllShardsAsync(IShardRouter shardRouter)
    {
        foreach (Continent continent in Enum.GetValues<Continent>())
        {
            var options = new DbContextOptionsBuilder<ArticleDbContext>()
                .UseSqlServer(shardRouter.GetConnectionString(continent))
                .Options;

            await using var db = new ArticleDbContext(options);
            await EnsureCreatedWithRetryAsync(db, continent);
        }
    }

    /// <summary>
    /// The SQL Server containers take a few seconds to accept connections
    /// after "docker compose up", and the target database (e.g.
    /// "ArticleDatabase") doesn't exist until EnsureCreatedAsync creates
    /// it - so a plain connectivity check against that database would
    /// fail with a login error even once the server is healthy. Instead,
    /// just retry EnsureCreatedAsync itself (it connects to "master" to
    /// issue CREATE DATABASE) until it succeeds or we give up.
    /// </summary>
    private static async Task EnsureCreatedWithRetryAsync(ArticleDbContext db, Continent continent)
    {
        const int maxAttempts = 15;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        throw new InvalidOperationException($"Database for continent '{continent}' never became reachable.");
    }
}
