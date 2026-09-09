using System.Text.RegularExpressions;
using ArticleService.Configuration;
using Microsoft.Data.SqlClient;

namespace ArticleService.Data;

public sealed class DatabaseInitializer
{
    private static readonly Regex SafeDatabaseName =
        new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    private readonly IShardConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IShardConnectionFactory connectionFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAllAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ArticleDatabase shard initialization.");

        // Every region has its own SQL Server container, so these eight tasks
        // do not contend with each other. Only one initializer PROCESS exists.
        await Task.WhenAll(
            ArticleRegions.All.Select(region => InitializeShardAsync(region, cancellationToken)));

        _logger.LogInformation("All ArticleDatabase shards are ready.");
    }

    private async Task InitializeShardAsync(string region, CancellationToken cancellationToken)
    {
        await WaitForSqlServerAsync(region, cancellationToken);

        await ExecuteWithRetryAsync(
            () => EnsureDatabaseExistsAsync(region, cancellationToken),
            region,
            "create database",
            cancellationToken);

        await ExecuteWithRetryAsync(
            () => EnsureArticlesTableExistsAsync(region, cancellationToken),
            region,
            "create Articles table",
            cancellationToken);

        _logger.LogInformation("Article shard {Region} is ready.", region);
    }

    private async Task WaitForSqlServerAsync(string region, CancellationToken cancellationToken)
    {
        const int attempts = 90;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var connection = _connectionFactory.CreateMasterConnection(region);
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (SqlException exception) when (attempt < attempts)
            {
                _logger.LogInformation(
                    "Waiting for SQL shard {Region} ({Attempt}/{Attempts}). SQL error {SqlError}: {Message}",
                    region,
                    attempt,
                    attempts,
                    exception.Number,
                    exception.Message);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task EnsureDatabaseExistsAsync(string region, CancellationToken cancellationToken)
    {
        var databaseName = _connectionFactory.DatabaseName;
        if (!SafeDatabaseName.IsMatch(databaseName))
        {
            throw new InvalidOperationException("Database name contains unsupported characters.");
        }

        // The database name cannot be parameterized in CREATE DATABASE, so it is
        // validated above before interpolation. DB_ID itself remains parameterized.
        var sql = $"""
            IF DB_ID(@databaseName) IS NULL
            BEGIN
                CREATE DATABASE [{databaseName}];
            END;
            """;

        await using var connection = _connectionFactory.CreateMasterConnection(region);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@databaseName", databaseName);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 1801)
        {
            // Idempotency safeguard: database already exists.
        }
    }

    private async Task EnsureArticlesTableExistsAsync(string region, CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.Articles', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Articles
                (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    Title NVARCHAR(300) NOT NULL,
                    Body NVARCHAR(MAX) NOT NULL,
                    Author NVARCHAR(200) NOT NULL,
                    Region NVARCHAR(32) NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL,
                    UpdatedAt DATETIMEOFFSET NOT NULL
                );
            END;
            """;

        await using var connection = _connectionFactory.CreateArticleConnection(region);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 2714)
        {
            // Idempotency safeguard: table already exists.
        }
    }

    private async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string region,
        string operationName,
        CancellationToken cancellationToken)
    {
        const int attempts = 15;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (SqlException exception) when (attempt < attempts && IsTransient(exception))
            {
                _logger.LogWarning(
                    "Transient SQL error while trying to {Operation} for {Region}. " +
                    "Attempt {Attempt}/{Attempts}, SQL error {SqlError}: {Message}",
                    operationName,
                    region,
                    attempt,
                    attempts,
                    exception.Number,
                    exception.Message);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private static bool IsTransient(SqlException exception) =>
        exception.Number is
            1205 or  // deadlock victim
            4060 or  // cannot open database yet
            233 or   // connection initialization/transient server state
            -2;      // command timeout
}
