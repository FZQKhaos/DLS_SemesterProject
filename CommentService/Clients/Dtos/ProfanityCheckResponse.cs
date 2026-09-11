namespace CommentService.Clients.Dtos;

public class ProfanityCheckResponse
{
    public bool IsProfane { get; set; }
    public List<string> MatchedWords { get; set; } = [];
}
