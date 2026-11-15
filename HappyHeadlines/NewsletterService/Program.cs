using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using Monitoring;
using NewsletterService;
using NewsletterService.Worker;
using StackExchange.Redis;

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

// The flag is read from Redis for each notification. No restart is necessary.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(
        builder.Configuration["Redis:ConnectionString"] ?? "redis:6379");
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddSingleton<SubscriberReleaseToggle>();
builder.Services.AddSingleton<SubscriberQueueBus>(_ => new SubscriberQueueBus(
    RabbitHutch.CreateBus(
        builder.Configuration["SubscriberQueue:ConnectionString"]
            ?? "host=subscriber-rabbitmq;username=appuser;password=apppassword",
        sr => sr.EnableSystemTextJson())));
builder.Services.AddHostedService<SubscriberQueueWorker>();

var app = builder.Build();
        app.UseHappyHeadlinesRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapSubscriberToggle();
app.Run();
