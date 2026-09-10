using ArticleService.Service.Dto;

namespace ArticleService.Service.Interface;

public interface IArticleService
{
    Task<ArticleResponseDto> CreateArticle(ArticleRequestDto dto);
    
    Task<ArticleResponseDto> GetArticleByIdAndContinent(string id, string continent);
    
    Task<ArticleResponseDto> UpdateArticle(string id, ArticleRequestDto dto);
    
    Task<bool> DeleteArticle(string id, string continent);
}