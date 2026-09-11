using Microsoft.EntityFrameworkCore;

namespace CommentService.Data;

/// <summary>
/// Creates the CommentDatabase schema on startup. CommentService only
/// ever runs as a single instance, so - unlike ArticleService's three
/// replicas - there's no risk of several instances racing each other to
/// create the same database, and no separate one-shot initializer
/// container is needed.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(CommentDbContext db)
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

        throw new InvalidOperationException("CommentDatabase never became reachable.");
    }
}
