using ArticleService.Data;
using ArticleService.Data.Interface;
using ArticleService.Service.Interface;
using ArticleService.Worker;
using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using Monitoring;

namespace ArticleService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.AddMonitoring("ArticleService");
        builder.Services.AddControllers();

        builder.Services.AddScoped<Coordinator>();
        builder.Services.AddScoped<IArticleService, Service.ArticleService>();
        builder.Services.AddScoped<IArticleRepository, ArticleRepository>();

        var rabbitMqConnectionString = builder.Configuration["RabbitMq:ConnectionString"]
            ?? "host=localhost;username=appuser;password=apppassword";
        builder.Services.AddSingleton<IBus>(_ =>
            RabbitHutch.CreateBus(
                rabbitMqConnectionString,
                serviceRegister => serviceRegister.EnableSystemTextJson()));
        builder.Services.AddHostedService<ArticlePublishedSubscriber>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();

        // app.UseHttpsRedirection();

        app.Run();
    }
}