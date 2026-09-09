namespace ArticleService.Models.Dtos;

/// <summary>
/// What a client sends to update an article. Continent is deliberately not
/// editable here - moving an article to another continent would mean moving
/// its row to a different physical database, which a plain update can't do.
/// </summary>
public class UpdateArticleRequest
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Author { get; set; }
}
