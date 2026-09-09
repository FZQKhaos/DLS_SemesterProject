using ArticleService.Data;
using ArticleService.Models;
using ArticleService.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Controllers;

/// <summary>
/// The REST API for articles. Continent is part of the route for
/// Read/Update/Delete because it tells the repository (and, underneath,
/// the shard router) which of the eight databases to look in - an id
/// alone is only unique within one continent's database, not globally.
/// </summary>
[ApiController]
[Route("api/articles")]
public class ArticlesController(IArticleRepository repository) : ControllerBase
{
    // POST api/articles
    [HttpPost]
    public async Task<ActionResult<Article>> Create(CreateArticleRequest request)
    {
        var article = new Article
        {
            Title = request.Title,
            Content = request.Content,
            Author = request.Author,
            Continent = request.Continent,
            PublishedAtUtc = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(article);

        return CreatedAtAction(
            nameof(Get),
            new { continent = created.Continent, id = created.Id },
            created);
    }

    // GET api/articles/{continent}
    // Bonus endpoint on top of the required four (Create/Read/Update/Delete):
    // lists every article in one continent's database, so the API is
    // browsable without already knowing an id.
    [HttpGet("{continent}")]
    public async Task<ActionResult<List<Article>>> GetAll(Continent continent)
    {
        return Ok(await repository.GetAllAsync(continent));
    }

    // GET api/articles/{continent}/{id}
    [HttpGet("{continent}/{id:int}")]
    public async Task<ActionResult<Article>> Get(Continent continent, int id)
    {
        var article = await repository.GetAsync(continent, id);
        return article is null ? NotFound() : Ok(article);
    }

    // PUT api/articles/{continent}/{id}
    [HttpPut("{continent}/{id:int}")]
    public async Task<ActionResult<Article>> Update(Continent continent, int id, UpdateArticleRequest request)
    {
        var updated = await repository.UpdateAsync(continent, id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    // DELETE api/articles/{continent}/{id}
    [HttpDelete("{continent}/{id:int}")]
    public async Task<IActionResult> Delete(Continent continent, int id)
    {
        var deleted = await repository.DeleteAsync(continent, id);
        return deleted ? NoContent() : NotFound();
    }
}
