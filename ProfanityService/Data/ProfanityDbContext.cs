using Microsoft.EntityFrameworkCore;
using ProfanityService.Models;

namespace ProfanityService.Data;

/// <summary>
/// EF Core's gateway to the ProfanityDatabase. This is a separate physical
/// database from CommentDatabase and ArticleDatabase - ProfanityService
/// owns it exclusively, nobody else is allowed to read or write it
/// directly. That's part of the fault isolation between CommentService
/// and ProfanityService: even a runaway query here can't touch comment
/// data, because there's no shared database to run it against.
/// </summary>
public class ProfanityDbContext(DbContextOptions<ProfanityDbContext> options) : DbContext(options)
{
    public DbSet<BannedWord> BannedWords => Set<BannedWord>();
}
