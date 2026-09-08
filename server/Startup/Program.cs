using Api.Rest;
using Application;
using Infrastructure.Postgres;
using NSwag.Generation;
using Startup.Documentation;

namespace Startup;

public class Program
{
    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();
        ConfigureServices(builder.Services, builder.Configuration);
        var app = builder.Build();
        await ConfigureMiddleware(app);
        await app.RunAsync();
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var appOptions = services.AddAppOptions(configuration);

        services.RegisterApplicationServices();

        services.AddDataSourceAndRepositories();
        
        services.RegisterRestApiServices();
        services.AddOpenApiDocument(conf =>
        {
            
        });
    }

    public static async Task ConfigureMiddleware(WebApplication app)
    {
        app.ConfigureRestApi();

        app.MapGet("Acceptance", () => "Accepted");

        app.UseOpenApi(conf => { conf.Path = "openapi/v1.json"; });

        var document = await app.Services.GetRequiredService<IOpenApiDocumentGenerator>().GenerateAsync("v1");
        var json = document.ToJson();
        await File.WriteAllTextAsync("openapi.json", json);

        app.GenerateTypeScriptClient("/../../client/src/generated-client.ts").GetAwaiter().GetResult();
    }
}