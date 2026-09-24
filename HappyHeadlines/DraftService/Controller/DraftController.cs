using DraftService.Service.Dto;
using DraftService.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Monitoring;

namespace DraftService.Controller;

[ApiController]
[Route("[controller]")]
public class DraftController(IDraftService draftService, ILogger<DraftController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateDraft([FromBody] DraftRequestDto dto)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity("DraftController.CreateDraft"))
        {
            logger.LogInformation(
                "Activity created: {ActivityCreated}",
                activity is not null);
            logger.LogInformation("Received request to create a draft with title: {Title}", dto.Title);
            var draft = await draftService.CreateDraft(dto);

            return Ok(draft);
        }
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDraft(string id)
    {
        using( var activity = MonitoringExtensions.ActivitySource.StartActivity("DraftController.GetDraft"))
        {
            activity?.SetTag("draft.id", id);
            logger.LogInformation("Received request to get a draft with id: {Id}", id);
            var draft = await draftService.GetDraftAsync(id);
            if (draft == null)
            {
                return NotFound();
            }
            
            logger.LogInformation("Draft with id: {Id} retrieved", id);
            return Ok(draft);
        }
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDraft(string id, [FromBody] DraftRequestDto dto)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity("DraftController.UpdateDraft"))
        {
            logger.LogInformation("Received request to update a draft with id: {Id}", id);
            var draft = await draftService.UpdateDraftAsync(id, dto);
            if (draft == null)
            {
                return NotFound();
            }

            logger.LogInformation("Draft with id: {Id} updated", id);
            return Ok(draft);
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDraft(string id)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity("DraftController.DeleteDraft"))
        {
            logger.LogInformation("Received request to delete a draft with id: {Id}", id);
            var success = await draftService.DeleteDraftAsync(id);
            if (!success)
            {
                return NotFound();
            }

            logger.LogInformation("Draft with id: {Id} deleted", id);
            return Ok();
        }
    }
}