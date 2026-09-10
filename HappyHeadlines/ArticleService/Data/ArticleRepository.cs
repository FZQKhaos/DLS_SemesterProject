using ArticleService.Data.Interface;
using ArticleService.Domain;
using ArticleService.Service.Dto;
using Npgsql;

namespace ArticleService.Data;

public class ArticleRepository(Coordinator coordinator) : IArticleRepository
{
    public async Task<Article> CreateArticle(Article article)
    {
        using var connection = coordinator.GetConnectionForContinent(article.Continent);
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO articles (id, title, body, continent)
              VALUES (@id, @title, @body, @continent)
              RETURNING id;",
            connection);

        cmd.Parameters.AddWithValue("id", article.Id);
        cmd.Parameters.AddWithValue("title", article.Title);
        cmd.Parameters.AddWithValue("body", article.Body);
        cmd.Parameters.AddWithValue("continent", article.Continent);

        await cmd.ExecuteScalarAsync();

        return article;
    }

    public async Task<Article?> GetArticleByIdAndContinent(string id, string continent)
    {
        using var connection = coordinator.GetConnectionForContinent(continent);
        using var cmd = new NpgsqlCommand(
            @"SELECT id, title, body, continent
              FROM articles
              WHERE id = @id AND continent = @continent;",
            connection);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("continent", continent);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new Article
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Body = reader.GetString(2),
            Continent = reader.GetString(3)
        };
    }

    public async Task<Article?> UpdateArticle(string id, Article article)
    {
        using var connection = coordinator.GetConnectionForContinent(article.Continent);
        using var cmd = new NpgsqlCommand(
            @"UPDATE articles
              SET title = @title, body = @body
              WHERE id = @id AND continent = @continent
              RETURNING id, title, body, continent;",
            connection);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("title", article.Title);
        cmd.Parameters.AddWithValue("body", article.Body);
        cmd.Parameters.AddWithValue("continent", article.Continent);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new Article
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Body = reader.GetString(2),
            Continent = reader.GetString(3)
        };
    }

    public async Task<bool> DeleteArticle(string id, string continent)
    {
        using var connection = coordinator.GetConnectionForContinent(continent);
        using var cmd = new NpgsqlCommand(
            @"DELETE FROM articles
              WHERE id = @id AND continent = @continent;",
            connection);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("continent", continent);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }
}