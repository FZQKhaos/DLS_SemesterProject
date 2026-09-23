using DraftService.Data.Interface;
using DraftService.Domain;
using Npgsql;

namespace DraftService.Data;

public class DraftRepository(NpgsqlDataSource dataSource) : IDraftRepository
{
    public async Task<Draft> CreateDraft(Draft draft)
    {
        const string sql = """
            INSERT INTO drafts (id, title, body, continent)
            VALUES ($1, $2, $3, $4)
            RETURNING id, title, body, continent;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(draft.Id);
        command.Parameters.AddWithValue(draft.Title);
        command.Parameters.AddWithValue(draft.Body);
        command.Parameters.AddWithValue(draft.Continent);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return ReadDraft(reader);
    }

    public async Task<Draft?> GetDraft(string id)
    {
        const string sql = """
            SELECT id, title, body, continent
            FROM drafts
            WHERE id = $1;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadDraft(reader) : null;
    }

    public async Task<Draft?> UpdateDraft(string id, Draft draft)
    {
        const string sql = """
            UPDATE drafts
            SET title = $2, body = $3, continent = $4
            WHERE id = $1
            RETURNING id, title, body, continent;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(draft.Title);
        command.Parameters.AddWithValue(draft.Body);
        command.Parameters.AddWithValue(draft.Continent);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadDraft(reader) : null;
    }

    public async Task<bool> DeleteDraft(string id)
    {
        const string sql = "DELETE FROM drafts WHERE id = $1;";

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(id);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private static Draft ReadDraft(NpgsqlDataReader reader)
    {
        return new Draft
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Body = reader.GetString(2),
            Continent = reader.GetString(3)
        };
    }
}