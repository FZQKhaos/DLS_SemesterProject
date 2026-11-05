using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using FeatureToggles;
using Monitoring;
using NewsletterService.Client;
using NewsletterService.Worker;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddMonitoring("NewsletterService");
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Release toggle (FeatureHub). Connects in the background and does not block startup.
builder.Services.AddFeatureToggles();

// Existing RabbitMQ (ArticlePublished). Unchanged.
var rabbitMqConnectionString = builder.Configuration["RabbitMq:ConnectionString"]
    ?? "host=localhost;username=appuser;password=apppassword";
builder.Services.AddSingleton<IBus>(_ =>
    RabbitHutch.CreateBus(
        rabbitMqConnectionString,
        serviceRegister => serviceRegister.EnableSystemTextJson()));
builder.Services.AddHostedService<ArticlePublishedSubscriber>();

// New: listens on SubscriberQueue (its own broker, reconnects on its own).
builder.Services.AddHostedService<SubscriberSubscribedConsumer>();

// Circuit breaker towards SubscriberService (same pattern as CommentService -> ProfanityService).
builder.Services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SubscriberCircuitBreaker");
    var failureCount = configuration.GetValue("SubscriberService:CircuitBreakerFailures", 3);
    var breakSeconds = configuration.GetValue("SubscriberService:CircuitBreakerSeconds", 30);

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TaskCanceledException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: failureCount,
            durationOfBreak: TimeSpan.FromSeconds(breakSeconds),
            onBreak: (outcome, breakDelay) =>
            {
                logger.LogWarning("SubscriberService circuit OPEN for {BreakSeconds}s after failure: {Failure}",
                    breakDelay.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            },
            onReset: () => logger.LogInformation("SubscriberService circuit CLOSED; normal calls resumed."),
            onHalfOpen: () => logger.LogInformation("SubscriberService circuit HALF-OPEN; probing dependency."));
});

builder.Services
    .AddHttpClient<ISubscriberClient, SubscriberClient>((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["SubscriberService:BaseUrl"] ?? "http://subscriber-service:8080";
        var timeoutSeconds = configuration.GetValue("SubscriberService:TimeoutSeconds", 2);
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    })
    .AddPolicyHandler((sp, _) => sp.GetRequiredService<IAsyncPolicy<HttpResponseMessage>>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.Run();
