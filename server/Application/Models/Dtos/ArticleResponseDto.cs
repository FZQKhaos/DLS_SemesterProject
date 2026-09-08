using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos;

public class ArticleResponseDto
{
    [Required]
    public string Id { get; set; }
    
    public string Author { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    
    public string Content { get; set; } = null!;
    
    public DateTime PublishedAt { get; set; }
    
    public string Category { get; set; } = null!;
}