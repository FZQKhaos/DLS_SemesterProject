using DraftService.Service.Dto;
using DraftService.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DraftService.Controller;

[ApiController]
[Route("[controller]")]
public class DraftController(IDraftService draftService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateDraft([FromBody] DraftRequestDto dto)
    {
        var draft = await draftService.CreateDraft(dto);
        return Ok(draft);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDraft(string id)
    {
        var draft = await draftService.GetDraftAsync(id);
        if (draft == null)
        {
            return NotFound();
        }
        return Ok(draft);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDraft(string id, [FromBody] DraftRequestDto dto)
    {
        var draft = await draftService.UpdateDraftAsync(id, dto);
        if (draft == null)
        {
            return NotFound();
        }
        return Ok(draft);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDraft(string id)
    {
        var success = await draftService.DeleteDraftAsync(id);
        if (!success)
        {
            return NotFound();
        }
        return Ok();
    }
}