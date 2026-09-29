using System.ComponentModel.DataAnnotations;

namespace PublisherService.Domain;

public class Article
{
    [Required]
    public required string Id { get; set; }

    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Body { get; set; }

    [Required]
    public required string Continent { get; set; }
}