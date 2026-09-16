using CommentService.Domain;
using CommentService.Service.Dto;
using CommentService.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CommentService.Controller;

[ApiController]
[Route("api/comments")]
public sealed class CommentController(ICommentService commentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CommentRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.ArticleId) || string.IsNullOrWhiteSpace(dto.Author) || string.IsNullOrWhiteSpace(dto.Body))
            return ValidationProblem(ModelState);

        var created = await commentService.CreateAsync(dto, cancellationToken);
        var routeValues = new { id = created.Id };

        if (created.ModerationStatus == ModerationStatuses.Pending)
            return AcceptedAtAction(nameof(GetById), routeValues, created);

        return CreatedAtAction(nameof(GetById), routeValues, created);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        return comment is null ? NotFound() : Ok(comment);
    }

    [HttpGet("article/{articleId}")]
    public async Task<IActionResult> GetByArticle(string articleId, [FromQuery] bool includePending = false, CancellationToken cancellationToken = default)
    {
        var comments = await commentService.GetByArticleAsync(articleId, includePending, cancellationToken);
        return Ok(comments);
    }
}
