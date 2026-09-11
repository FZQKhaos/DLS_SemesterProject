using Microsoft.EntityFrameworkCore;
using ProfanityService.Models;

namespace ProfanityService.Data;

public class BannedWordRepository(ProfanityDbContext db) : IBannedWordRepository
{
    public async Task<List<BannedWord>> GetAllAsync() =>
        await db.BannedWords.OrderBy(w => w.Word).ToListAsync();

    public async Task<BannedWord> AddAsync(string word)
    {
        var bannedWord = new BannedWord { Word = word };
        db.BannedWords.Add(bannedWord);
        await db.SaveChangesAsync();
        return bannedWord;
    }
}
