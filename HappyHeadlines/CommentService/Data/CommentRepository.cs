using CommentService.Data.Interface;
using CommentService.Domain;
using Npgsql;

namespace CommentService.Data;

public sealed class CommentRepository(NpgsqlDataSource dataSource) : ICommentRepository
{
    public async Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO comments
                (id, article_id, author, original_body, body, moderation_status, created_at, updated_at)
            VALUES
                (@id, @articleId, @author, @originalBody, @body, @status, @createdAt, @updatedAt);", connection);

        cmd.Parameters.AddWithValue("id", comment.Id);
        cmd.Parameters.AddWithValue("articleId", comment.ArticleId);
        cmd.Parameters.AddWithValue("author", comment.Author);
        cmd.Parameters.AddWithValue("originalBody", comment.OriginalBody);
        cmd.Parameters.AddWithValue("body", comment.Body);
        cmd.Parameters.AddWithValue("status", comment.ModerationStatus);
        cmd.Parameters.AddWithValue("createdAt", comment.CreatedAt);
        cmd.Parameters.AddWithValue("updatedAt", comment.UpdatedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return comment;
    }

    public async Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            SELECT id, article_id, author, original_body, body, moderation_status, created_at, updated_at
            FROM comments WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadComment(reader) : null;
    }

    public async Task<IReadOnlyList<Comment>> GetByArticleAsync(string articleId, bool includePending, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var sql = includePending
            ? @"SELECT id, article_id, author, original_body, body, moderation_status, created_at, updated_at
                FROM comments WHERE article_id = @articleId ORDER BY created_at ASC;"
            : @"SELECT id, article_id, author, original_body, body, moderation_status, created_at, updated_at
                FROM comments WHERE article_id = @articleId AND moderation_status = 'published' ORDER BY created_at ASC;";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("articleId", articleId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var comments = new List<Comment>();
        while (await reader.ReadAsync(cancellationToken)) comments.Add(ReadComment(reader));
        return comments;
    }

    public async Task<IReadOnlyList<Comment>> GetPendingAsync(int limit, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            SELECT id, article_id, author, original_body, body, moderation_status, created_at, updated_at
            FROM comments
            WHERE moderation_status = 'pending'
            ORDER BY created_at ASC
            LIMIT @limit;", connection);
        cmd.Parameters.AddWithValue("limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var comments = new List<Comment>();
        while (await reader.ReadAsync(cancellationToken)) comments.Add(ReadComment(reader));
        return comments;
    }

    public async Task<bool> MarkPublishedAsync(string id, string filteredBody, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE comments
            SET body = @body, moderation_status = 'published', updated_at = @updatedAt
            WHERE id = @id AND moderation_status = 'pending';", connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("body", filteredBody);
        cmd.Parameters.AddWithValue("updatedAt", DateTimeOffset.UtcNow);
        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Comment ReadComment(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(0),
        ArticleId = reader.GetString(1),
        Author = reader.GetString(2),
        OriginalBody = reader.GetString(3),
        Body = reader.GetString(4),
        ModerationStatus = reader.GetString(5),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(6),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(7)
    };
}
