using System.ComponentModel.DataAnnotations;

namespace PublisherService.Service.Dto;

public sealed class PublishArticleRequestDto
{
    [Required]
    public required string Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [Required]
    public required string Body { get; init; }

    [Required]
    public required string Continent { get; init; }
}
