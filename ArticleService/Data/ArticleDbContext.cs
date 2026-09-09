using ArticleService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Data;

/// <summary>
/// EF Core's gateway to ONE physical database. It knows nothing about
/// continents or sharding - it just talks to whatever connection string
/// it was constructed with. The same class is reused for all eight
/// databases; only the connection string differs (see ShardRouter).
/// </summary>
public class ArticleDbContext(DbContextOptions<ArticleDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            // Store the enum as its name ("Europe") instead of a number,
            // so the database rows stay readable during debugging/demo.
            entity.Property(a => a.Continent).HasConversion<string>();
        });
    }
}
