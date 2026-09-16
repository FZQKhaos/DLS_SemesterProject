using CommentService.Domain;

namespace CommentService.Data.Interface;

public interface ICommentRepository
{
    Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken = default);
    Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Comment>> GetByArticleAsync(string articleId, bool includePending, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Comment>> GetPendingAsync(int limit, CancellationToken cancellationToken = default);
    Task<bool> MarkPublishedAsync(string id, string filteredBody, CancellationToken cancellationToken = default);
}
