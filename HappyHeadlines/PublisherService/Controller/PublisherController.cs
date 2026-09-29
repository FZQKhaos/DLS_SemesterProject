using Microsoft.AspNetCore.Mvc;
using PublisherService.Service.Dto;

namespace PublisherService.Controller;

[ApiController]
[Route("[controller]")]
public class PublisherController(PublisherService.Service.PublisherService publisherService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishArticleRequestDto request)
    {
        await publisherService.PublishAsync(request);
        return Accepted(new { request.Id, Status = "queued" });
    }
}