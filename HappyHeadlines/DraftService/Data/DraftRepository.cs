using DraftService.Data.Interface;
using DraftService.Domain;
using Monitoring;
using Npgsql;

namespace DraftService.Data;

public class DraftRepository(NpgsqlDataSource dataSource, ILogger<DraftRepository> logger) : IDraftRepository
{
    public async Task<Draft> CreateDraft(Draft draft)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            try
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
            catch (NpgsqlException ex)
            {
                logger.LogError(ex,
                    "Failed to create draft with id: {Id}", draft.Id);
                throw;
            }
        }
    }

    public async Task<Draft?> GetDraft(string id)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            try
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
            catch (NpgsqlException ex)
            {
                logger.LogError(ex, "Failed to retrieve draft with id: {Id}", id);
                throw;
            }
        }
    }

    public async Task<Draft?> UpdateDraft(string id, Draft draft)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            try
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
            catch (NpgsqlException ex)
            {
                logger.LogError(ex, "Failed to update draft with id: {Id}", id);
                throw;
            }
        }
    }

    public async Task<bool> DeleteDraft(string id)
    {
        using (var activity = MonitoringExtensions.ActivitySource.StartActivity())
        {
            try
            {
                const string sql = "DELETE FROM drafts WHERE id = $1;";

                await using var command = dataSource.CreateCommand(sql);

                command.Parameters.AddWithValue(id);

                return await command.ExecuteNonQueryAsync() == 1;
            }
            catch (NpgsqlException ex)
            {
                logger.LogError(ex, "Failed to delete draft with id: {Id}", id);
                throw;
            }
        }
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