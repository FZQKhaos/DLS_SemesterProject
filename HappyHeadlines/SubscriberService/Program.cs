using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using FeatureToggles;
using Monitoring;
using Npgsql;
using SubscriberService.Data;
using SubscriberService.Data.Interface;
using SubscriberService.Service;
using SubscriberService.Service.Interface;

var builder = WebApplication.CreateBuilder(args);

builder.AddMonitoring("SubscriberService");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Release toggle (FeatureHub). Connects in the background and does not block startup.
builder.Services.AddFeatureToggles();

// SubscriberDatabase: its own Postgres, only reachable inside the subscriber lane.
var connectionString = builder.Configuration.GetConnectionString("SubscriberDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:SubscriberDatabase is missing.");
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

// SubscriberQueue: its own RabbitMQ broker (not the one used for ArticlePublished).
var queueConnectionString = builder.Configuration["RabbitMq:ConnectionString"]
    ?? "host=localhost;port=5673;username=appuser;password=apppassword";
builder.Services.AddSingleton<IBus>(_ =>
    RabbitHutch.CreateBus(
        queueConnectionString,
        serviceRegister => serviceRegister.EnableSystemTextJson()));

builder.Services.AddScoped<ISubscriberRepository, SubscriberRepository>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// TOGGLE POINT 1: one place at the edge of the service.
// IsEnabled() is called on EVERY request, so a change in FeatureHub takes effect immediately
// without redeploy and without restart.
var toggles = app.Services.GetRequiredService<IFeatureToggles>();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isInfrastructure = path.StartsWithSegments("/health") || path.StartsWithSegments("/swagger");

    if (!isInfrastructure && !toggles.IsEnabled(FeatureKeys.SubscriberService))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { error = "SubscriberService is disabled by release toggle." });
        return;
    }

    await next();
});

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
