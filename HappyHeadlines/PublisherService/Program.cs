using EasyNetQ;
using Monitoring;

var builder = WebApplication.CreateBuilder(args);

builder.AddMonitoring("PublisherService");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration["RabbitMq:ConnectionString"]
    ?? "host=localhost;username=appuser;password=apppassword";
builder.Services.AddSingleton<IBus>(_ =>
    RabbitHutch.CreateBus(connectionString, serviceRegister => serviceRegister.EnableSystemTextJson()));
builder.Services.AddSingleton<PublisherService.Service.ArticleQueuePublisher>();
builder.Services.AddScoped<PublisherService.Service.PublisherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
