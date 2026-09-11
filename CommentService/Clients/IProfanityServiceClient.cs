namespace CommentService.Clients;

public interface IProfanityServiceClient
{
    Task<ProfanityCheckResult> CheckAsync(string text);
}
