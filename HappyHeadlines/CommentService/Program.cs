using System.Net;
using CommentService.Cache;
using CommentService.Client;
using CommentService.Client.Interface;
using CommentService.Data;
using CommentService.Data.Interface;
using CommentService.Service.Interface;
using CommentService.Worker;
using Npgsql;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

namespace CommentService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        var commentConnectionString = builder.Configuration.GetConnectionString("CommentDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:CommentDatabase is missing.");
        builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(commentConnectionString));
        // CommentService uses Redis for cache-aside comment lists and LRU state. Redis
        // improves repeated reads, while CommentDatabase remains the source of truth.
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(options);
        });
        // The cache stores all comments for each of the 30 most recently accessed
        // articles; CommentService filters pending comments after loading the cached set.
        builder.Services.AddSingleton<ICommentCache, RedisCommentCache>();
        builder.Services.AddScoped<ICommentRepository, CommentRepository>();
        builder.Services.AddScoped<ICommentService, Service.CommentService>();

        builder.Services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ProfanityCircuitBreaker");
            var failureCount = configuration.GetValue("ProfanityService:CircuitBreakerFailures", 3);
            var breakSeconds = configuration.GetValue("ProfanityService:CircuitBreakerSeconds", 30);

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: failureCount,
                    durationOfBreak: TimeSpan.FromSeconds(breakSeconds),
                    onBreak: (outcome, breakDelay) =>
                    {
                        logger.LogWarning("ProfanityService circuit OPEN for {BreakSeconds}s after failure: {Failure}",
                            breakDelay.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    },
                    onReset: () => logger.LogInformation("ProfanityService circuit CLOSED; normal calls resumed."),
                    onHalfOpen: () => logger.LogInformation("ProfanityService circuit HALF-OPEN; probing dependency."));
        });

        builder.Services
            .AddHttpClient<IProfanityClient, ProfanityClient>((sp, client) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var baseUrl = configuration["ProfanityService:BaseUrl"] ?? "http://profanity-service:8080";
                var timeoutSeconds = configuration.GetValue("ProfanityService:TimeoutSeconds", 2);
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddPolicyHandler((sp, _) => sp.GetRequiredService<IAsyncPolicy<HttpResponseMessage>>());

        builder.Services.AddHostedService<PendingModerationWorker>();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.MapHealthChecks("/health");
        app.Run();
    }
}
