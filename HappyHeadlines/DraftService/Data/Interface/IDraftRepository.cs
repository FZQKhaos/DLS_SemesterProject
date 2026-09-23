using DraftService.Domain;
namespace DraftService.Data.Interface;

public interface IDraftRepository
{
    Task<Draft> CreateDraft(Draft draft);
    
    Task<Draft?> GetDraft(string id);
    
    Task<Draft?> UpdateDraft(string id, Draft draft);
    
    Task<bool> DeleteDraft(string id);
}