using System.ComponentModel.DataAnnotations;

namespace ArticleService.Models;

public sealed class UpdateArticleRequest
{
    [Required]
    [StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Body { get; init; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Author { get; init; } = string.Empty;
}
