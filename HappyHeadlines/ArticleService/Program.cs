using ArticleService.Data;
using ArticleService.Data.Interface;
using ArticleService.Service.Interface;

namespace ArticleService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        builder.Services.AddScoped<Coordinator>();
        builder.Services.AddScoped<IArticleService, Service.ArticleService>();
        builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
        
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