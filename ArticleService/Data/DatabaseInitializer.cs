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
    /// <summary>
    /// A handful of demo articles, one per continent, so the API returns
    /// something immediately after "docker compose up" instead of every
    /// database starting out empty. Purely for demoing/testing - not
    /// required by the assignment's four CRUD endpoints.
    /// </summary>
    private static readonly (Continent Continent, string Title, string Content)[] SeedArticles =
    [
        (Continent.Europe, "Artikel 1", "Denne artikel virker - Europe-databasen svarer."),
        (Continent.Asia, "Artikel 2", "Denne artikel virker - Asia-databasen svarer."),
        (Continent.Africa, "Artikel 3", "Denne artikel virker - Africa-databasen svarer."),
        (Continent.NorthAmerica, "Artikel 4", "Denne artikel virker - NorthAmerica-databasen svarer."),
        (Continent.Global, "Artikel 5", "Denne artikel virker - Global-databasen svarer.")
    ];

    public static async Task InitializeAllShardsAsync(IShardRouter shardRouter)
    {
        foreach (Continent continent in Enum.GetValues<Continent>())
        {
            var options = new DbContextOptionsBuilder<ArticleDbContext>()
                .UseSqlServer(shardRouter.GetConnectionString(continent))
                .Options;

            await using var db = new ArticleDbContext(options);
            await EnsureCreatedWithRetryAsync(db, continent);
            await SeedIfEmptyAsync(db, continent);
        }
    }

    /// <summary>
    /// Inserts this continent's demo article(s) the first time only - if
    /// the table already has rows (e.g. a re-run against an existing
    /// volume), it does nothing, so restarting the stack never duplicates
    /// seed data.
    /// </summary>
    private static async Task SeedIfEmptyAsync(ArticleDbContext db, Continent continent)
    {
        if (await db.Articles.AnyAsync())
        {
            return;
        }

        foreach (var seed in SeedArticles.Where(s => s.Continent == continent))
        {
            db.Articles.Add(new Article
            {
                Title = seed.Title,
                Content = seed.Content,
                Author = "Seed",
                Continent = continent,
                PublishedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
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
