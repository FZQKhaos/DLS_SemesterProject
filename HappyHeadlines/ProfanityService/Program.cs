using Npgsql;
using ProfanityService.Data;
using ProfanityService.Data.Interface;
using ProfanityService.Service.Interface;

namespace ProfanityService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        var connectionString = builder.Configuration.GetConnectionString("ProfanityDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:ProfanityDatabase is missing.");
        builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        builder.Services.AddScoped<IProfanityRepository, ProfanityRepository>();
        builder.Services.AddScoped<IProfanityService, Service.ProfanityService>();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.MapHealthChecks("/health");
        app.Run();
    }
}
