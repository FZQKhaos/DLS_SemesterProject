using Npgsql;
using DraftService.Data;
using DraftService.Data.Interface;
using DraftService.Service.Interface;
using Monitoring;
using Serilog;

namespace DraftService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.AddMonitoring("DraftService");
        
        builder.Services.AddControllers();
        
        var connectionString = builder.Configuration.GetConnectionString("DraftDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:DraftDatabase is missing.");
        builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        
        builder.Services.AddScoped<IDraftService, Service.DraftService>();
        builder.Services.AddScoped<IDraftRepository, DraftRepository>();
        
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
        
        //app.UseHttpsRedirection();
        try
        {
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
        }
        
    }
}


