using CommentService.Models;

namespace CommentService.Data;

public interface ICommentRepository
{
    Task<Comment> CreateAsync(Comment comment);
    Task<List<Comment>> GetAllForArticleAsync(int articleId);
    Task<Comment?> GetAsync(int id);
    Task<bool> DeleteAsync(int id);
}
