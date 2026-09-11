using Microsoft.EntityFrameworkCore;
using ProfanityService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ProfanityDbContext>(options =>
    options.UseSqlServer(BuildConnectionString(builder.Configuration)));
builder.Services.AddScoped<IBannedWordRepository, BannedWordRepository>();

var app = builder.Build();

// Single instance, so it's safe to create/seed the database on every
// startup - unlike ArticleService's three replicas, there's nobody to race.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProfanityDbContext>();
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

static string BuildConnectionString(IConfiguration configuration)
{
    var host = configuration["Database:Host"]
        ?? throw new InvalidOperationException("Missing configuration value 'Database:Host'.");
    var user = configuration["Database:User"];
    var password = configuration["Database:Password"];
    var name = configuration["Database:Name"];

    return $"Server={host};Database={name};User Id={user};Password={password};TrustServerCertificate=True";
}
