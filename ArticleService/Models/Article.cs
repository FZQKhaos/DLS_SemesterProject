namespace ArticleService.Models;

public class Article
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Author { get; set; }
    public Continent Continent { get; set; }
    public DateTime PublishedAtUtc { get; set; }
}
