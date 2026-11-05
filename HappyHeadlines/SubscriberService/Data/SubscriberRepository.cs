using Monitoring;
using Npgsql;
using SubscriberService.Data.Interface;
using SubscriberService.Domain;

namespace SubscriberService.Data;

public sealed class SubscriberRepository(
    NpgsqlDataSource dataSource,
    ILogger<SubscriberRepository> logger) : ISubscriberRepository
{
    public async Task<Subscriber?> AddAsync(Subscriber subscriber, CancellationToken cancellationToken = default)
    {
        using var activity = MonitoringExtensions.ActivitySource.StartActivity();
        try
        {
            const string sql = """
                               INSERT INTO subscribers (id, email, subscribed_at_utc)
                               VALUES ($1, $2, $3)
                               ON CONFLICT (email) DO NOTHING
                               RETURNING id, email, subscribed_at_utc;
                               """;

            await using var command = dataSource.CreateCommand(sql);
            command.Parameters.AddWithValue(subscriber.Id);
            command.Parameters.AddWithValue(subscriber.Email);
            command.Parameters.AddWithValue(subscriber.SubscribedAtUtc);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadSubscriber(reader) : null;
        }
        catch (NpgsqlException ex)
        {
            // Log the id, not the email: email addresses are personal data and do not belong in logs.
            logger.LogError(ex, "Failed to add subscriber with id: {Id}", subscriber.Id);
            throw;
        }
    }

    public async Task<bool> RemoveByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var activity = MonitoringExtensions.ActivitySource.StartActivity();
        try
        {
            const string sql = "DELETE FROM subscribers WHERE email = $1;";

            await using var command = dataSource.CreateCommand(sql);
            command.Parameters.AddWithValue(email);

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(ex, "Failed to remove a subscriber.");
            throw;
        }
    }

    public async Task<IReadOnlyList<Subscriber>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var activity = MonitoringExtensions.ActivitySource.StartActivity();
        try
        {
            const string sql = """
                               SELECT id, email, subscribed_at_utc
                               FROM subscribers
                               ORDER BY subscribed_at_utc;
                               """;

            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var result = new List<Subscriber>();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(ReadSubscriber(reader));
            }

            return result;
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(ex, "Failed to list subscribers.");
            throw;
        }
    }

    private static Subscriber ReadSubscriber(NpgsqlDataReader reader)
    {
        return new Subscriber
        {
            Id = reader.GetString(0),
            Email = reader.GetString(1),
            SubscribedAtUtc = reader.GetDateTime(2)
        };
    }
}
