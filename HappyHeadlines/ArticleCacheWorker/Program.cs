using ArticleService.Cache;
using ArticleService.Data;
using ArticleService.Data.Interface;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<Coordinator>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddSingleton<IArticleCache, RedisArticleCache>();
builder.Services.AddHostedService<ArticleCacheWorker>();

var host = builder.Build();
await host.RunAsync();
