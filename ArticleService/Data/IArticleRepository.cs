using ArticleService.Models;

namespace ArticleService.Data;

public interface IArticleRepository
{
    Task<Article> CreateAsync(string region, CreateArticleRequest request, CancellationToken cancellationToken);
    Task<Article?> GetAsync(string region, Guid id, CancellationToken cancellationToken);
    Task<Article?> UpdateAsync(string region, Guid id, UpdateArticleRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string region, Guid id, CancellationToken cancellationToken);
}
