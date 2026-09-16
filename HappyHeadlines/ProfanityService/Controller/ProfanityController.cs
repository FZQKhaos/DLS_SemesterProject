using Microsoft.AspNetCore.Mvc;
using ProfanityService.Service.Dto;
using ProfanityService.Service.Interface;

namespace ProfanityService.Controller;

[ApiController]
[Route("api/profanity")]
public sealed class ProfanityController(IProfanityService profanityService) : ControllerBase
{
    [HttpPost("filter")]
    public async Task<IActionResult> Filter([FromBody] FilterRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.Text))
            return ValidationProblem(ModelState);

        return Ok(await profanityService.FilterAsync(dto.Text, cancellationToken));
    }
}
