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
        var cachedComments = await commentCache.GetCommentsAsync(articleId, cancellationToken);
        if (cachedComments is not null)
        {
            return FilterForCaller(cachedComments, includePending).Select(Map).ToList();
        }

        var comments = await repository.GetByArticleAsync(articleId, includePending: true, cancellationToken);
        await commentCache.SetCommentsAsync(articleId, comments, cancellationToken);
        return FilterForCaller(comments, includePending).Select(Map).ToList();
    }

    private static IReadOnlyList<Comment> FilterForCaller(IReadOnlyList<Comment> comments, bool includePending)
    {
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
