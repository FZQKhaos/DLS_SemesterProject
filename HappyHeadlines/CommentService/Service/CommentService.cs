using CommentService.Cache;
using CommentService.Client.Interface;
using CommentService.Data.Interface;
using CommentService.Domain;
using CommentService.Service.Dto;
using CommentService.Service.Interface;

namespace CommentService.Service;

public sealed class CommentService(
    ICommentRepository repository,
    IProfanityClient profanityClient,
    ICommentCache commentCache,
    ILogger<CommentService> logger) : ICommentService
{
    public async Task<CommentResponseDto> CreateAsync(CommentRequestDto dto, CancellationToken cancellationToken = default)
    {
        var moderation = await profanityClient.FilterAsync(dto.Body, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var published = moderation.ServiceAvailable;

        var comment = new Comment
        {
            Id = Guid.NewGuid().ToString(),
            ArticleId = dto.ArticleId.Trim(),
            Author = dto.Author.Trim(),
            OriginalBody = dto.Body,
            Body = published ? moderation.FilteredText : dto.Body,
            ModerationStatus = published ? ModerationStatuses.Published : ModerationStatuses.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.CreateAsync(comment, cancellationToken);
        // Cache invalidation avoids stale comment lists. After a new comment is stored,
        // any cached list for the article is no longer complete, so the simple strategy
        // is: remove now, reload from the database on the next cache miss.
        await commentCache.RemoveCommentsAsync(comment.ArticleId, cancellationToken);
        return Map(comment);
    }

    public async Task<CommentResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var comment = await repository.GetByIdAsync(id, cancellationToken);
        return comment is null ? null : Map(comment);
    }

    public async Task<IReadOnlyList<CommentResponseDto>> GetByArticleAsync(string articleId, bool includePending, CancellationToken cancellationToken = default)
    {
        // CommentCache uses cache-aside:
        // 1. Try Redis first.
        // 2. On hit, return cached data.
        // 3. On miss, query CommentDatabase.
        // 4. Store the database result in Redis so the next request can become a hit.
        //
        // This contrasts with ArticleCache, which is populated proactively by
        // ArticleCacheWorker instead of by the user request that observed the miss.
        var cachedComments = await commentCache.GetCommentsAsync(articleId, cancellationToken);
        if (cachedComments is not null)
        {
            // The cache stores the complete comment set for the article. includePending
            // is a response/filtering concern, so it is applied after cache retrieval.
            return FilterForCaller(cachedComments, includePending).Select(Map).ToList();
        }

        // The assignment asks for "all comments for the most recently 30 accessed
        // articles", so cache population must load pending and published comments.
        // If the first request used includePending=false and only published comments
        // were cached, a later includePending=true request could never recover the
        // pending comments from that cached entry.
        var comments = await repository.GetByArticleAsync(articleId, includePending: true, cancellationToken);
        await commentCache.SetCommentsAsync(articleId, comments, cancellationToken);
        return FilterForCaller(comments, includePending).Select(Map).ToList();
    }

    private static IReadOnlyList<Comment> FilterForCaller(IReadOnlyList<Comment> comments, bool includePending)
    {
        // Filtering after cache population lets different callers reuse the same cached
        // data while still respecting whether the response should expose pending comments.
        if (includePending)
            return comments;

        return comments.Where(comment => comment.ModerationStatus == ModerationStatuses.Published).ToList();
    }

    private static CommentResponseDto Map(Comment comment) => new()
    {
        Id = comment.Id,
        ArticleId = comment.ArticleId,
        Author = comment.Author,
        Body = comment.ModerationStatus == ModerationStatuses.Pending ? "[pending moderation]" : comment.Body,
        ModerationStatus = comment.ModerationStatus,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt
    };
}
