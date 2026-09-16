using Application.Models.Dtos;
using Application.Models.Dtos.Article;

namespace Application.Interfaces;

public interface IArticleService
{
    public Task<ArticleResponseDto> GetArticlesAsync();
    
    public Task<ArticleResponseDto> CreateArticleAsync(ArticleRequestDto article);
    
    public Task<ArticleResponseDto> UpdateArticleAsync(ArticleRequestDto article);
    
    public Task<ArticleResponseDto> DeleteArticleAsync(int id);
}