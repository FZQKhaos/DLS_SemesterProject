using ArticleService.Configuration;
using ArticleService.Data;
using ArticleService.Models;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Controllers;

[ApiController]
[Route("articles/{region}")]
public sealed class ArticlesController : ControllerBase
{
    private readonly IArticleRepository _repository;

    public ArticlesController(IArticleRepository repository)
    {
        _repository = repository;
    }

    // CREATE
    [HttpPost]
    [ProducesResponseType(typeof(Article), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Article>> Create(
        string region,
        [FromBody] CreateArticleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetRegion(region, out var normalized, out var error))
        {
            return error!;
        }

        var article = await _repository.CreateAsync(normalized, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { region = normalized, id = article.Id }, article);
    }

    // READ
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Article), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Article>> Get(
        string region,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetRegion(region, out var normalized, out var error))
        {
            return error!;
        }

        var article = await _repository.GetAsync(normalized, id, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    // UPDATE
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Article), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Article>> Update(
        string region,
        Guid id,
        [FromBody] UpdateArticleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetRegion(region, out var normalized, out var error))
        {
            return error!;
        }

        var article = await _repository.UpdateAsync(normalized, id, request, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    // DELETE
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        string region,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetRegion(region, out var normalized, out var error))
        {
            return error!;
        }

        var deleted = await _repository.DeleteAsync(normalized, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private bool TryGetRegion(
        string region,
        out string normalized,
        out ActionResult? error)
    {
        if (ArticleRegions.TryNormalize(region, out normalized))
        {
            error = null;
            return true;
        }

        error = BadRequest(new
        {
            message = $"Unknown article region '{region}'.",
            validRegions = ArticleRegions.All
        });

        return false;
    }
}
