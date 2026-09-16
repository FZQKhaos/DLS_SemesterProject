using CommentService.Service.Dto;

namespace CommentService.Service.Interface;

public interface ICommentService
{
    Task<CommentResponseDto> CreateAsync(CommentRequestDto dto, CancellationToken cancellationToken = default);
    Task<CommentResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentResponseDto>> GetByArticleAsync(string articleId, bool includePending, CancellationToken cancellationToken = default);
}
