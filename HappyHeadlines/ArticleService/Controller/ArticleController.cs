using ArticleService.Service.Dto;
using ArticleService.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Controller;

[ApiController]
[Route("[controller]")]
public class ArticleController(IArticleService articleService) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> CreateArticle([FromBody] ArticleRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body) || string.IsNullOrWhiteSpace(dto.Continent))
            return BadRequest();

        var article = await articleService.CreateArticle(dto);
        return Ok(article);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetArticleByIdAndContinent(string id, string continent)
    {
        var article = await articleService.GetArticleByIdAndContinent(id, continent);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateArticle(string id, [FromBody] ArticleRequestDto dto)
    {
        var article = await articleService.UpdateArticle(id, dto);
        return article is null ? NotFound() : Ok(article);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string continent)
    {
        var deleted = await articleService.DeleteArticle(id, continent);
        return deleted ? NoContent() : NotFound();
    }
}