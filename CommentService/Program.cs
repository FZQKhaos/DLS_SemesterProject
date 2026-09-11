using CommentService.Clients;
using CommentService.Data;
using Microsoft.EntityFrameworkCore;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseSqlServer(BuildConnectionString(builder.Configuration)));
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

// Swimlane isolation (see the illustration in the assignment notes):
// ProfanityService gets its own dedicated, named HttpClient - its own
// connection pool and its own short timeout - registered via
// AddHttpClient so a slow or dead ProfanityService can never starve
// resources CommentService needs for anything else (its own database
// calls, its other endpoints). The circuit breaker below is scoped to
// this HttpClient alone: if ProfanityService starts failing, only calls
// to *it* get short-circuited - the rest of CommentService keeps running.
builder.Services.AddHttpClient<IProfanityServiceClient, ProfanityServiceClient>((services, client) =>
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["ProfanityService:BaseUrl"]
            ?? throw new InvalidOperationException("Missing configuration value 'ProfanityService:BaseUrl'.");
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(3);
    })
    .AddPolicyHandler((services, _) => BuildCircuitBreakerPolicy(services));

var app = builder.Build();

// Single instance, so it's safe to create the database on every startup -
// unlike ArticleService's three replicas, there's nobody to race.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommentDbContext>();
    await DatabaseInitializer.InitializeAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
return;

// Trips after 3 consecutive failed/erroring calls to ProfanityService and
// then fails every call immediately (without even attempting the HTTP
// request) for 30 seconds, before letting a single trial call through to
// see if ProfanityService has recovered. ProfanityServiceClient catches
// the resulting BrokenCircuitException and falls back to letting the
// comment through unchecked - that fallback is the circuit breaker
// "taking over" while ProfanityService is unavailable.
static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ProfanityServiceCircuitBreaker");

    return Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .OrResult(response => (int)response.StatusCode >= 500)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) => logger.LogWarning(
                "ProfanityService circuit breaker OPEN for {BreakDelay} - failing fast instead of calling it ({Reason}).",
                breakDelay, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()),
            onReset: () => logger.LogInformation(
                "ProfanityService circuit breaker CLOSED - calls are reaching it normally again."),
            onHalfOpen: () => logger.LogInformation(
                "ProfanityService circuit breaker HALF-OPEN - letting one trial call through."));
}

static string BuildConnectionString(IConfiguration configuration)
{
    var host = configuration["Database:Host"]
        ?? throw new InvalidOperationException("Missing configuration value 'Database:Host'.");
    var user = configuration["Database:User"];
    var password = configuration["Database:Password"];
    var name = configuration["Database:Name"];

    return $"Server={host};Database={name};User Id={user};Password={password};TrustServerCertificate=True";
}
