using ArticleService.Cache;
using ArticleService.Data;
using ArticleService.Data.Interface;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    // The worker writes the proactive ArticleCache into the same Redis instance that
    // ArticleService later reads from during global article requests.
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<Coordinator>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
// ArticleCacheWorker, not ArticleService, populates ArticleCache. This keeps cache
// refresh as a background/offline concern instead of request-time cache-aside logic.
builder.Services.AddSingleton<IArticleCache, RedisArticleCache>();
builder.Services.AddHostedService<ArticleCacheWorker>();

var host = builder.Build();
await host.RunAsync();
