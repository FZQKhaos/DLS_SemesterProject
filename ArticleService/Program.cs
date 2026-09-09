using System.Text.Json.Serialization;
using ArticleService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // Serialize/accept the Continent enum as text ("Europe") in JSON
    // instead of a number, so requests and responses stay human-readable.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// One IShardRouter per process is enough - the connection strings it
// builds never change while the app is running.
builder.Services.AddSingleton<IShardRouter, ShardRouter>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();

var app = builder.Build();

// docker-compose runs one short-lived container with Database:InitializeOnly=true
// to create the schema in all eight databases before the three real
// ArticleService replicas (which have this flag unset/false) start up.
if (app.Configuration.GetValue<bool>("Database:InitializeOnly"))
{
    var shardRouter = app.Services.GetRequiredService<IShardRouter>();
    await DatabaseInitializer.InitializeAllShardsAsync(shardRouter);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
