using System.Net.Mail;
using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using Events;
using Npgsql;
using StackExchange.Redis;
using SubscriberService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("SubscriberDatabase")
    ?? throw new InvalidOperationException("Subscriber database connection missing")));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    CreateRedis(builder.Configuration["Redis:ConnectionString"] ?? "redis:6379"));
builder.Services.AddSingleton<RuntimeSubscriberToggle>();
// Two independent brokers: ArticlePublished is read from the article bus;
// notification messages are written to the dedicated subscriber queue broker.
builder.Services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus(
    builder.Configuration["RabbitMq:ConnectionString"]
        ?? "host=rabbitmq;username=appuser;password=apppassword",
    sr => sr.EnableSystemTextJson()));
builder.Services.AddSingleton<SubscriberQueueBus>(_ => new SubscriberQueueBus(
    RabbitHutch.CreateBus(
        builder.Configuration["SubscriberQueue:ConnectionString"]
            ?? "host=subscriber-rabbitmq;username=appuser;password=apppassword",
        sr => sr.EnableSystemTextJson())));
builder.Services.AddHostedService<ArticleFanoutWorker>();

var app = builder.Build();
app.MapHealthChecks("/health");

// Fail closed: disabled (or Redis unavailable) behaves as if this API is absent.
app.MapPost("/api/subscribers", async (
    SubscribeRequest request, RuntimeSubscriberToggle toggle, NpgsqlDataSource db,
    SubscriberQueueBus queue, ILogger<Program> logger, CancellationToken ct) =>
{
    if (!await toggle.IsEnabledAsync()) return Results.NotFound();
    var email = request.Email?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !MailAddress.TryCreate(email, out _))
        return Results.BadRequest(new { error = "Valid email required" });

    await using var connection = await db.OpenConnectionAsync(ct);
    await using var cmd = new NpgsqlCommand(
        "INSERT INTO subscribers (email) VALUES (@email) ON CONFLICT DO NOTHING", connection);
    cmd.Parameters.AddWithValue("email", email);
    var added = await cmd.ExecuteNonQueryAsync(ct) > 0;
    if (added)
    {
        // A broker failure must not invalidate a committed subscriber record.
        // See docs/assignment2-patch.md: production reliability requires a transactional outbox.
        try
        {
            await queue.Bus.PubSub.PublishAsync(new SubscriberNotification
            {
                NotificationId = $"welcome:{email}",
                Email = email,
                Subject = "Welcome to Happy Headlines",
                Body = "Thank you for subscribing to Happy Headlines!",
                Kind = "welcome"
            });
        }
        catch (Exception ex) { logger.LogError(ex, "Welcome notification could not be queued for {Email}", email); }
    }
    return Results.Ok(new { email, subscribed = true, created = added });
});

app.MapDelete("/api/subscribers/{email}", async (
    string email, RuntimeSubscriberToggle toggle, NpgsqlDataSource db, CancellationToken ct) =>
{
    if (!await toggle.IsEnabledAsync()) return Results.NotFound();
    await using var connection = await db.OpenConnectionAsync(ct);
    await using var cmd = new NpgsqlCommand("DELETE FROM subscribers WHERE email = @email", connection);
    cmd.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
    return await cmd.ExecuteNonQueryAsync(ct) > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

static IConnectionMultiplexer CreateRedis(string connectionString)
{
    var options = ConfigurationOptions.Parse(connectionString);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
}

public sealed record SubscribeRequest(string? Email);
