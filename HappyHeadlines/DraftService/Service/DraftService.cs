using DraftService.Data.Interface;
using DraftService.Domain;
using DraftService.Service.Dto;
using DraftService.Service.Interface;
using Monitoring;

namespace DraftService.Service;

public class DraftService(IDraftRepository repo, ILogger<DraftService> logger) : IDraftService
{
    public async Task<DraftResponseDto?> CreateDraft(DraftRequestDto dto)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            var id = Guid.NewGuid().ToString();

            var draft = new Draft()
            {
                Id = id,
                Title = dto.Title,
                Body = dto.Body,
                Continent = dto.Continent
            };

            var createdDraft = await repo.CreateDraft(draft);
            logger.LogInformation("Draft with id: {Id} created", id);

            return new DraftResponseDto
            {
                Id = createdDraft.Id,
                Title = createdDraft.Title,
                Body = createdDraft.Body,
                Continent = createdDraft.Continent
            };
        }
    }

    public async Task<DraftResponseDto?> GetDraftAsync(string id)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            var draft = await repo.GetDraft(id);
            if (draft == null)
            {
                logger.LogError("Draft with id: {Id} not found", id);
                throw new InvalidOperationException("Draft not found");
            }

            logger.LogInformation("Draft with id: {Id} retrieved", id);
            return new DraftResponseDto
            {
                Id = draft.Id,
                Title = draft.Title,
                Body = draft.Body,
                Continent = draft.Continent
            };
        }
    }

    public async Task<DraftResponseDto?> UpdateDraftAsync(string id, DraftRequestDto dto)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            var draft = await repo.GetDraft(id);
            if (draft == null)
            {
                throw new InvalidOperationException("Draft not found");
            }

            draft.Title = dto.Title;
            draft.Body = dto.Body;
            draft.Continent = dto.Continent;

            var updatedDraft = await repo.UpdateDraft(id, draft);
            if (updatedDraft == null)
            {
                throw new InvalidOperationException("Failed to update draft");
            }

            logger.LogInformation("Draft with id: {Id} updated", id);

            return new DraftResponseDto
            {
                Id = updatedDraft.Id,
                Title = updatedDraft.Title,
                Body = updatedDraft.Body,
                Continent = updatedDraft.Continent
            };
        }
    }

    public async Task<bool> DeleteDraftAsync(string id)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            var draft = await repo.GetDraft(id);
            if (draft == null)
            {
                throw new InvalidOperationException("Draft not found");
            }

            logger.LogInformation("Draft with id: {Id} deleted", id);
            return await repo.DeleteDraft(id);
        }
    }
}