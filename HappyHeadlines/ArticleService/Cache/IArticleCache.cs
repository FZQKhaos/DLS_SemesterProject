using ArticleService.Domain;

namespace ArticleService.Cache;

public interface IArticleCache
{
    Task<Article?> GetArticleAsync(string id, CancellationToken cancellationToken = default);

    Task SetArticleAsync(string id, Article article, TimeSpan expiration, CancellationToken cancellationToken = default);
}
