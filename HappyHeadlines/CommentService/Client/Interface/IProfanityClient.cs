using CommentService.Client.Dto;

namespace CommentService.Client.Interface;

public interface IProfanityClient
{
    Task<ProfanityFilterResponse> FilterAsync(string text, CancellationToken cancellationToken = default);
}
