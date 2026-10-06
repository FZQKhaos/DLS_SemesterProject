using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using Monitoring;
using NewsletterService.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddMonitoring("NewsletterService");
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var rabbitMqConnectionString = builder.Configuration["RabbitMq:ConnectionString"]
    ?? "host=localhost;username=appuser;password=apppassword";
builder.Services.AddSingleton<IBus>(_ =>
    RabbitHutch.CreateBus(
        rabbitMqConnectionString,
        serviceRegister => serviceRegister.EnableSystemTextJson()));
builder.Services.AddHostedService<ArticlePublishedSubscriber>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.Run();
