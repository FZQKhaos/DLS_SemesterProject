using ProfanityService.Models;

namespace ProfanityService.Data;

public interface IBannedWordRepository
{
    Task<List<BannedWord>> GetAllAsync();
    Task<BannedWord> AddAsync(string word);
}
