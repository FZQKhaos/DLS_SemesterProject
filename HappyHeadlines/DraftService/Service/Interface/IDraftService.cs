using DraftService.Service.Dto;

namespace DraftService.Service.Interface;

public interface IDraftService
{
    Task<DraftResponseDto?> CreateDraft(DraftRequestDto dto);
    Task<DraftResponseDto?> GetDraftAsync(string id);
    Task<DraftResponseDto?> UpdateDraftAsync(string id, DraftRequestDto dto);
    Task<bool> DeleteDraftAsync(string id);
}