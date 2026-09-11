using CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Data;

public class CommentRepository(CommentDbContext db) : ICommentRepository
{
    public async Task<Comment> CreateAsync(Comment comment)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return comment;
    }

    public async Task<List<Comment>> GetAllForArticleAsync(int articleId) =>
        await db.Comments
            .Where(c => c.ArticleId == articleId)
            .OrderBy(c => c.Id)
            .ToListAsync();

    public async Task<Comment?> GetAsync(int id) =>
        await db.Comments.FindAsync(id);

    public async Task<bool> DeleteAsync(int id)
    {
        var comment = await db.Comments.FindAsync(id);
        if (comment is null)
        {
            return false;
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return true;
    }
}
