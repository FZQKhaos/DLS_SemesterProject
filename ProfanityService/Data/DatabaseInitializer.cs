using Microsoft.EntityFrameworkCore;
using ProfanityService.Models;

namespace ProfanityService.Data;

/// <summary>
/// Creates the ProfanityDatabase schema and seeds a small demo word list
/// on startup. Unlike ArticleService's DatabaseInitializer, this doesn't
/// need its own one-shot container: ProfanityService only ever runs as a
/// single instance, so there's no risk of several replicas racing each
/// other to create the same database.
/// </summary>
public static class DatabaseInitializer
{
    private static readonly string[] SeedWords = ["idiot", "stupid", "dumb", "moron"];

    public static async Task InitializeAsync(ProfanityDbContext db)
    {
        await EnsureCreatedWithRetryAsync(db);

        if (!await db.BannedWords.AnyAsync())
        {
            db.BannedWords.AddRange(SeedWords.Select(word => new BannedWord { Word = word }));
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// The SQL Server container takes a few seconds to accept connections
    /// after "docker compose up", and the ProfanityDatabase doesn't exist
    /// until EnsureCreatedAsync creates it - so retry until the server is
    /// actually reachable instead of failing on the first attempt.
    /// </summary>
    private static async Task EnsureCreatedWithRetryAsync(ProfanityDbContext db)
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

        throw new InvalidOperationException("ProfanityDatabase never became reachable.");
    }
}
