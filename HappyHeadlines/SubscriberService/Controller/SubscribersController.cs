using Microsoft.AspNetCore.Mvc;
using SubscriberService.Service.Dto;
using SubscriberService.Service.Interface;

namespace SubscriberService.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class SubscribersController(ISubscriptionService subscriptionService) : ControllerBase
{
    /// <summary>The website calls this on "Subscribe to newsletters".</summary>
    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await subscriptionService.SubscribeAsync(request.Email, cancellationToken);
        return result is null
            ? Conflict(new { error = "Already subscribed." })
            : StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>The website calls this on "Unsubscribe from newsletters".</summary>
    [HttpDelete("{email}")]
    public async Task<IActionResult> Unsubscribe(string email, CancellationToken cancellationToken)
    {
        return await subscriptionService.UnsubscribeAsync(email, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    /// <summary>NewsletterService calls this ("Sends request to get subscribers").</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriberResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await subscriptionService.GetAllAsync(cancellationToken));
    }
}
