using ArticleService.Models;
using ArticleService.Models.Dtos;

namespace ArticleService.Data;

public interface IArticleRepository
{
    Task<Article> CreateAsync(Article article);
    Task<Article?> GetAsync(Continent continent, int id);
    Task<Article?> UpdateAsync(Continent continent, int id, UpdateArticleRequest request);
    Task<bool> DeleteAsync(Continent continent, int id);
}
