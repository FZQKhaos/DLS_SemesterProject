using ArticleService.Domain;

namespace ArticleService.Data.Interface;

public interface IArticleRepository
{
    Task<Article> CreateArticle(Article article);

    Task SavePublishedArticle(Article article, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Article>> GetArticlesPublishedSince(
        string continent,
        DateTime publishedSinceUtc,
        CancellationToken cancellationToken = default);
    
    Task<Article?> GetArticleByIdAndContinent(string id, string continent);
    
    Task<Article?> UpdateArticle(string id, Article article);
    
    Task<bool> DeleteArticle(string id, string continent);
}