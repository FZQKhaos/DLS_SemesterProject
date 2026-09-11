using CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Data;

/// <summary>
/// EF Core's gateway to the CommentDatabase. A separate physical database
/// from ProfanityDatabase and ArticleDatabase - CommentService owns it
/// exclusively. That separation is half of the fault isolation between
/// CommentService and ProfanityService: neither service can take the
/// other's database down with it, because they don't share one.
/// </summary>
public class CommentDbContext(DbContextOptions<CommentDbContext> options) : DbContext(options)
{
    public DbSet<Comment> Comments => Set<Comment>();
}
