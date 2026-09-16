using Npgsql;
using ProfanityService.Data.Interface;

namespace ProfanityService.Data;

public sealed class ProfanityRepository(NpgsqlDataSource dataSource) : IProfanityRepository
{
    public async Task<IReadOnlyList<string>> GetAllWordsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT word FROM profanity_words ORDER BY LENGTH(word) DESC, word ASC;", connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var words = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            words.Add(reader.GetString(0));
        return words;
    }
}
