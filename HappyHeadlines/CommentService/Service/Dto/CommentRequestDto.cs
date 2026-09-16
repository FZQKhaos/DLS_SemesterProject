using System.ComponentModel.DataAnnotations;

namespace CommentService.Service.Dto;

public sealed class CommentRequestDto
{
    [Required]
    public string ArticleId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Body { get; set; } = string.Empty;
}
