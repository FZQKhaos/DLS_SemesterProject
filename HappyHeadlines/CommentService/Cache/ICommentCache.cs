using CommentService.Domain;

namespace CommentService.Cache;

public interface ICommentCache
{
    Task<IReadOnlyList<Comment>?> GetCommentsAsync(string articleId, CancellationToken cancellationToken = default);
    Task SetCommentsAsync(string articleId, IReadOnlyList<Comment> comments, CancellationToken cancellationToken = default);
    Task RemoveCommentsAsync(string articleId, CancellationToken cancellationToken = default);
    Task<bool> IsArticleCachedAsync(string articleId, CancellationToken cancellationToken = default);
}
