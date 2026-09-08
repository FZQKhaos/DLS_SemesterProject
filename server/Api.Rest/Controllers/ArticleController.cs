using Api.Rest.Extensions;
using Application.Interfaces;
using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Rest.Controllers;

[ApiController]
public class ArticleController(IArticleService articleService) : ControllerBase
{
    public const string ControllerRoute = "api/auth/";

    public const string CreateRoute = ControllerRoute + nameof(CreateArticle);
    
    public const string RegisterRoute = ControllerRoute + nameof(ReadArticle);
    
    public const string UpdateRoute = ControllerRoute + nameof(UpdateArticle);
    
    public const string DeleteRoute = ControllerRoute + nameof(DeleteArticle);
    


    [HttpPost]
    [Route(CreateRoute)]
    public async ActionResult<ArticleDto> CreateArticle([FromBody] ArticleDto dto)
    {
        return Ok(articleService.CreateArticleAsync(dto));
    }
    
    [HttpGet]
    [Route(RegisterRoute)]
    public async ActionResult<ArticleDto> ReadArticle([FromBody] ArticleDto dto)
    {
        return Ok(articleService.GetArticlesAsync());
    }
    
    [HttpPut]
    [Route(UpdateRoute)]
    public async Task<ActionResult<ArticleDto>> UpdateArticle([FromBody] ArticleDto dto)
    {
        return Ok(articleService.UpdateArticleAsync(dto));
    }
    
    [HttpDelete]
    [Route(DeleteRoute)]
    public async Task<ActionResult<ArticleDto>> DeleteArticle([FromBody] ArticleDto dto)
    {
        return Ok(await articleService.DeleteArticleAsync(dto.Id));
    }
}