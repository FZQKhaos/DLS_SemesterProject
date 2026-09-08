using Application.Interfaces;
using Application.Models.Dtos;
using Application.Models.Dtos.Article;
using Microsoft.AspNetCore.Mvc;

namespace Api.Rest.Controllers;

[ApiController]
public class ArticleController(IArticleService articleService) : ControllerBase
{
    public const string ControllerRoute = "api/articles/";

    public const string CreateRoute = ControllerRoute + nameof(CreateArticle);
    
    public const string RegisterRoute = ControllerRoute + nameof(ReadArticle);
    
    public const string UpdateRoute = ControllerRoute + nameof(UpdateArticle);
    
    public const string DeleteRoute = ControllerRoute + nameof(DeleteArticle);
    


    [HttpPost]
    [Route(CreateRoute)]
    public async Task<ActionResult<ArticleResponseDto>> CreateArticle([FromBody] ArticleRequestDto dto)
    {
        var response = await articleService.CreateArticleAsync(dto);
        return Ok(response);
    }
    
    [HttpGet]
    [Route(RegisterRoute)]
    public async Task<ActionResult<ArticleResponseDto>> ReadArticle([FromBody] ArticleRequestDto dto)
    {
        return Ok(await articleService.GetArticlesAsync());
    }
    
    [HttpPut]
    [Route(UpdateRoute)]
    public async Task<ActionResult<ArticleResponseDto>> UpdateArticle([FromBody] ArticleRequestDto dto)
    {
        return Ok(await articleService.UpdateArticleAsync(dto));
    }
    
    [HttpDelete("{id}")]
    [Route(DeleteRoute)]
    public async Task<IActionResult> DeleteArticle([FromRoute] int id)
    {
        return Ok(await articleService.DeleteArticleAsync(id));
    }
}