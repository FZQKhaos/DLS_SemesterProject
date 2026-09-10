using System.ComponentModel.DataAnnotations;

namespace ArticleService.Domain;

public sealed class Article
{
    [Required]
    public string Id { get; set; }
    
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Body { get; set; }
    
    [Required]
    public string Continent { get; set; }
}