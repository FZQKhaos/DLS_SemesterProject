namespace CommentService.Models;

public class Comment
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public required string Author { get; set; }
    public required string Text { get; set; }
    public DateTime PostedAtUtc { get; set; }

    /// <summary>
    /// False when this comment was let through *without* a profanity
    /// check, because ProfanityService was unreachable or its circuit
    /// breaker was open at the time - see ProfanityServiceClient. Kept on
    /// the row so it's visible afterwards which comments were never
    /// actually verified.
    /// </summary>
    public bool ProfanityChecked { get; set; }
}
