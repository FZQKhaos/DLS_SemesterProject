using Microsoft.AspNetCore.Mvc;
using ProfanityService.Data;
using ProfanityService.Models.Dtos;

namespace ProfanityService.Controllers;

/// <summary>
/// The REST API CommentService talks to directly (no gateway/UI in
/// between - see CommentService/Clients/ProfanityServiceClient.cs) to
/// check a comment's text before it gets stored.
/// </summary>
[ApiController]
[Route("api/profanity")]
public class ProfanityController(IBannedWordRepository repository) : ControllerBase
{
    // POST api/profanity/check
    [HttpPost("check")]
    public async Task<ActionResult<ProfanityCheckResponse>> Check(ProfanityCheckRequest request)
    {
        var bannedWords = await repository.GetAllAsync();
        var matches = ProfanityChecker.FindMatches(request.Text, bannedWords.Select(w => w.Word));

        return Ok(new ProfanityCheckResponse
        {
            IsProfane = matches.Count > 0,
            MatchedWords = matches
        });
    }

    // GET api/profanity/words
    // Lets you inspect/demo the banned-word list the check above uses.
    [HttpGet("words")]
    public async Task<ActionResult<List<string>>> GetWords()
    {
        var words = await repository.GetAllAsync();
        return Ok(words.Select(w => w.Word).ToList());
    }

    // POST api/profanity/words
    [HttpPost("words")]
    public async Task<ActionResult<string>> AddWord(AddBannedWordRequest request)
    {
        var added = await repository.AddAsync(request.Word);
        return CreatedAtAction(nameof(GetWords), null, added.Word);
    }
}
