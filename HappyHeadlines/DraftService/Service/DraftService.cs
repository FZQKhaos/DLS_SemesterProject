using DraftService.Data.Interface;
using DraftService.Domain;
using DraftService.Service.Dto;
using DraftService.Service.Interface;

namespace DraftService.Service;

public class DraftService(IDraftRepository draftRepository) : IDraftService
{
    public async Task<DraftResponseDto?> CreateDraft(DraftRequestDto dto)
    {
        var id = Guid.NewGuid().ToString();
        
        var draft = new Draft()
        {
            Id = id,
            Title = dto.Title,
            Body = dto.Body,
            Continent = dto.Continent
        };
        
        var createdDraft = await draftRepository.CreateDraft(draft);
        
        return new DraftResponseDto
        {
            Id = createdDraft.Id,
            Title = createdDraft.Title,
            Body = createdDraft.Body,
            Continent = createdDraft.Continent
        };
    }

    public async Task<DraftResponseDto?> GetDraftAsync(string id)
    {
        var draft = await draftRepository.GetDraft(id);
        if (draft == null) {
           throw new InvalidOperationException("Draft not found");
        }

        return new DraftResponseDto
        {
            Id = draft.Id,
            Title = draft.Title,
            Body = draft.Body,
            Continent = draft.Continent
        };
    }

    public async Task<DraftResponseDto?> UpdateDraftAsync(string id, DraftRequestDto dto)
    {
        var draft = await draftRepository.GetDraft(id);
        if (draft == null) {
            throw new InvalidOperationException("Draft not found");
        }

        draft.Title = dto.Title;
        draft.Body = dto.Body;
        draft.Continent = dto.Continent;

        var updatedDraft = await draftRepository.UpdateDraft(id, draft);
        if (updatedDraft == null) {
            throw new InvalidOperationException("Failed to update draft");
        }

        return new DraftResponseDto
        {
            Id = updatedDraft.Id,
            Title = updatedDraft.Title,
            Body = updatedDraft.Body,
            Continent = updatedDraft.Continent
        };
    }

    public async Task<bool> DeleteDraftAsync(string id)
    {
        var draft = await draftRepository.GetDraft(id);
        if (draft == null) {
            throw new InvalidOperationException("Draft not found");
        }
        
        return await draftRepository.DeleteDraft(id);
    }
}