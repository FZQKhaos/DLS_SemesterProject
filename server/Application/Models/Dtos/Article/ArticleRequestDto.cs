namespace Application.Models.Dtos;

public class ArticleRequestDto
{
    public string Author { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    
    public string Content { get; set; } = null!;
    
    public DateTime PublishedAt { get; set; }
    
    public string Category { get; set; } = null!;
}   