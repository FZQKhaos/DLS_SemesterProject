namespace ArticleService.Models.Dtos;

/// <summary>
/// What a client sends to create an article. Continent is included here
/// because it decides which of the eight databases the article is written to.
/// </summary>
public class CreateArticleRequest
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Author { get; set; }
    public Continent Continent { get; set; }
}
