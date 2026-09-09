using ArticleService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IShardConnectionFactory, ShardConnectionFactory>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddSingleton<DatabaseInitializer>();

var app = builder.Build();

// Database creation/schema setup is deliberately performed by one dedicated
// Docker Compose service. The three API replicas must NOT all run migrations
// concurrently, because concurrent CREATE DATABASE operations can deadlock in
// SQL Server's system databases.
if (builder.Configuration.GetValue<bool>("Database:InitializeOnly"))
{
    var initializer = app.Services.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAllAsync(CancellationToken.None);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Makes the x-axis split visible while testing through the load balancer.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Article-Service-Instance"] =
        Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "article-service";

    await next();
});

app.UseAuthorization();

// Friendly entry point when opening http://localhost:8080 in a browser.
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "article-service"
}));

app.MapControllers();

app.Run();
