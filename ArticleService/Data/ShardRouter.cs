using ArticleService.Models;

namespace ArticleService.Data;

/// <summary>
/// Builds one SQL Server connection string per continent from configuration.
/// In docker-compose.yml, "Shards:Europe" etc. are set to the *hostname*
/// of that continent's container (e.g. "article-db-europe") - Docker's
/// built-in DNS then resolves that name to the right container's IP.
/// The user/password/database name are shared across all eight servers,
/// only the host differs.
/// </summary>
public class ShardRouter : IShardRouter
{
    private readonly Dictionary<Continent, string> _connectionStrings;

    public ShardRouter(IConfiguration configuration)
    {
        var user = configuration["Database:User"];
        var password = configuration["Database:Password"];
        var databaseName = configuration["Database:Name"];

        _connectionStrings = new Dictionary<Continent, string>();

        foreach (Continent continent in Enum.GetValues<Continent>())
        {
            var host = configuration[$"Shards:{continent}"]
                ?? throw new InvalidOperationException(
                    $"Missing configuration value 'Shards:{continent}' - every continent needs a database host.");

            _connectionStrings[continent] =
                $"Server={host};Database={databaseName};User Id={user};Password={password};" +
                "TrustServerCertificate=True";
        }
    }

    public string GetConnectionString(Continent continent) => _connectionStrings[continent];
}
