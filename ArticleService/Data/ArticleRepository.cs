using ArticleService.Models;
using Microsoft.Data.SqlClient;

namespace ArticleService.Data;

public sealed class ArticleRepository : IArticleRepository
{
    private readonly IShardConnectionFactory _connectionFactory;

    public ArticleRepository(IShardConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Article> CreateAsync(
        string region,
        CreateArticleRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        const string sql = """
            INSERT INTO dbo.Articles (Id, Title, Body, Author, Region, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Body, INSERTED.Author,
                   INSERTED.Region, INSERTED.CreatedAt, INSERTED.UpdatedAt
            VALUES (@id, @title, @body, @author, @region, @createdAt, @updatedAt);
            """;

        await using var connection = _connectionFactory.CreateArticleConnection(region);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@title", request.Title);
        command.Parameters.AddWithValue("@body", request.Body);
        command.Parameters.AddWithValue("@author", request.Author);
        command.Parameters.AddWithValue("@region", region);
        command.Parameters.AddWithValue("@createdAt", now);
        command.Parameters.AddWithValue("@updatedAt", now);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return MapArticle(reader);
    }

    public async Task<Article?> GetAsync(
        string region,
        Guid id,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, Title, Body, Author, Region, CreatedAt, UpdatedAt
            FROM dbo.Articles
            WHERE Id = @id;
            """;

        await using var connection = _connectionFactory.CreateArticleConnection(region);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapArticle(reader) : null;
    }

    public async Task<Article?> UpdateAsync(
        string region,
        Guid id,
        UpdateArticleRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Articles
            SET Title = @title,
                Body = @body,
                Author = @author,
                UpdatedAt = @updatedAt
            OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Body, INSERTED.Author,
                   INSERTED.Region, INSERTED.CreatedAt, INSERTED.UpdatedAt
            WHERE Id = @id;
            """;

        await using var connection = _connectionFactory.CreateArticleConnection(region);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@title", request.Title);
        command.Parameters.AddWithValue("@body", request.Body);
        command.Parameters.AddWithValue("@author", request.Author);
        command.Parameters.AddWithValue("@updatedAt", DateTimeOffset.UtcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapArticle(reader) : null;
    }

    public async Task<bool> DeleteAsync(
        string region,
        Guid id,
        CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.Articles WHERE Id = @id;";

        await using var connection = _connectionFactory.CreateArticleConnection(region);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static Article MapArticle(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        Title = reader.GetString(1),
        Body = reader.GetString(2),
        Author = reader.GetString(3),
        Region = reader.GetString(4),
        CreatedAt = reader.GetDateTimeOffset(5),
        UpdatedAt = reader.GetDateTimeOffset(6)
    };
}
