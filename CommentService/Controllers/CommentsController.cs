using CommentService.Clients;
using CommentService.Data;
using CommentService.Models;
using CommentService.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommentService.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController(ICommentRepository repository, IProfanityServiceClient profanityClient)
    : ControllerBase
{
    // POST api/comments
    // Calls ProfanityService directly before storing anything. If it's
    // reachable and flags the text, the comment is rejected outright. If
    // it can't be reached (including: its circuit breaker is open), the
    // comment is stored anyway but marked ProfanityChecked = false - a
    // ProfanityService outage degrades moderation, it doesn't take
    // comments down with it.
    [HttpPost]
    public async Task<ActionResult<Comment>> Create(CreateCommentRequest request)
    {
        var profanityResult = await profanityClient.CheckAsync(request.Text);

        if (profanityResult.Checked && profanityResult.IsProfane)
        {
            return BadRequest("Comment rejected: contains profanity.");
        }

        var comment = new Comment
        {
            ArticleId = request.ArticleId,
            Author = request.Author,
            Text = request.Text,
            PostedAtUtc = DateTime.UtcNow,
            ProfanityChecked = profanityResult.Checked
        };

        var created = await repository.CreateAsync(comment);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    // GET api/comments/article/{articleId}
    [HttpGet("article/{articleId:int}")]
    public async Task<ActionResult<List<Comment>>> GetAllForArticle(int articleId)
    {
        return Ok(await repository.GetAllForArticleAsync(articleId));
    }

    // GET api/comments/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Comment>> Get(int id)
    {
        var comment = await repository.GetAsync(id);
        return comment is null ? NotFound() : Ok(comment);
    }

    // DELETE api/comments/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
