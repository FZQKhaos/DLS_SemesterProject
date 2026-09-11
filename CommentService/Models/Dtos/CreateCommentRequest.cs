namespace CommentService.Models.Dtos;

public class CreateCommentRequest
{
    public int ArticleId { get; set; }
    public required string Author { get; set; }
    public required string Text { get; set; }
}
