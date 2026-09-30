using System.Data;
using ArticleService.Data.Interface;
using ArticleService.Domain;
using ArticleService.Service.Dto;
using Npgsql;

namespace ArticleService.Data;

public class ArticleRepository(Coordinator coordinator) : IArticleRepository
{
    public async Task<Article> CreateArticle(Article article)
    {
        var connection = coordinator.GetConnectionForContinent(article.Continent);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO articles (id, title, body, continent, published_at)
              VALUES (@id, @title, @body, @continent, @published_at)
              RETURNING id;",
            connection);

        cmd.Parameters.AddWithValue("id", article.Id);
        cmd.Parameters.AddWithValue("title", article.Title);
        cmd.Parameters.AddWithValue("body", article.Body);
        cmd.Parameters.AddWithValue("continent", article.Continent);
        cmd.Parameters.AddWithValue("published_at", article.PublishedAtUtc);

        await cmd.ExecuteScalarAsync();

        return article;
    }

    public async Task SavePublishedArticle(Article article, CancellationToken cancellationToken = default)
    {
        var connection = coordinator.GetConnectionForContinent(article.Continent);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO articles (id, title, body, continent, published_at)
              VALUES (@id, @title, @body, @continent, @published_at)
              ON CONFLICT (id) DO NOTHING;",
            connection);

        cmd.Parameters.AddWithValue("id", article.Id);
        cmd.Parameters.AddWithValue("title", article.Title);
        cmd.Parameters.AddWithValue("body", article.Body);
        cmd.Parameters.AddWithValue("continent", article.Continent);
        cmd.Parameters.AddWithValue("published_at", article.PublishedAtUtc);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Article>> GetArticlesPublishedSince(
        string continent,
        DateTime publishedSinceUtc,
        CancellationToken cancellationToken = default)
    {
        var connection = coordinator.GetConnectionForContinent(continent);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"SELECT id, title, body, continent, published_at
              FROM articles
              WHERE continent = @continent
                AND published_at >= @published_since
              ORDER BY published_at DESC;",
            connection);

        cmd.Parameters.AddWithValue("continent", continent);
        cmd.Parameters.AddWithValue("published_since", publishedSinceUtc);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var articles = new List<Article>();

        while (await reader.ReadAsync(cancellationToken))
        {
            articles.Add(new Article
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Body = reader.GetString(2),
                Continent = reader.GetString(3),
                PublishedAtUtc = reader.GetDateTime(4)
            });
        }

        return articles;
    }

    public async Task<Article?> GetArticleByIdAndContinent(string id, string continent)
    {
        var connection = coordinator.GetConnectionForContinent(continent);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"SELECT id, title, body, continent, published_at
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
            Continent = reader.GetString(3),
            PublishedAtUtc = reader.GetDateTime(4)
        };
    }

    public async Task<Article?> UpdateArticle(string id, Article article)
    {
        var connection = coordinator.GetConnectionForContinent(article.Continent);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"UPDATE articles
              SET title = @title, body = @body
              WHERE id = @id AND continent = @continent
              RETURNING id, title, body, continent, published_at;",
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
            Continent = reader.GetString(3),
            PublishedAtUtc = reader.GetDateTime(4)
        };
    }

    public async Task<bool> DeleteArticle(string id, string continent)
    {
        var connection = coordinator.GetConnectionForContinent(continent);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
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