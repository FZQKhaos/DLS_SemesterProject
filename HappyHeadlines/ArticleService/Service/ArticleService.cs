using ArticleService.Data;
using ArticleService.Data.Interface;
using ArticleService.Domain;
using ArticleService.Service.Dto;
using ArticleService.Service.Interface;

namespace ArticleService.Service;

public class ArticleService(IArticleRepository repo) : IArticleService
{
    public async Task<ArticleResponseDto> CreateArticle(ArticleRequestDto dto)
    {
        var id = Guid.NewGuid().ToString();
        
        
        var article = new Article()
        {
            Id = id,
            Title = dto.Title,
            Body = dto.Body,
            Continent = dto.Continent
        };
        
        var createdArticle = await repo.CreateArticle(article);

        return new ArticleResponseDto
        {
            Id = createdArticle.Id,
            Title = createdArticle.Title,
            Body = createdArticle.Body,
            Continent = createdArticle.Continent
        };
        
    }

    public async Task<ArticleResponseDto> GetArticleByIdAndContinent(string id, string continent)
    {
        var article = await repo.GetArticleByIdAndContinent(id, continent);
        if (article == null)
            throw new InvalidOperationException("Article not found");

        return new ArticleResponseDto
        {
            Id = article.Id,
            Title = article.Title,
            Body = article.Body,
            Continent = article.Continent
        };
    }

    public async Task<ArticleResponseDto> UpdateArticle(string id, ArticleRequestDto dto)
    {
        var article = await repo.GetArticleByIdAndContinent(id, dto.Continent);
        if (article == null)
            throw new InvalidOperationException("Article not found");

        article.Title = dto.Title;
        article.Body = dto.Body;

        var updatedArticle = await repo.UpdateArticle(id, article);

        return new ArticleResponseDto
        {
            Id = updatedArticle.Id,
            Title = updatedArticle.Title,
            Body = updatedArticle.Body,
            Continent = updatedArticle.Continent
        };
    }

    public async Task<bool> DeleteArticle(string id, string continent)
    {
        var article = await repo.GetArticleByIdAndContinent(id, continent);
        if (article == null)
            throw new InvalidOperationException("Article not found");

        return await repo.DeleteArticle(id, continent);
    }
}