using ArticleService.Configuration;
using Microsoft.Data.SqlClient;

namespace ArticleService.Data;

public sealed class ShardConnectionFactory : IShardConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly string _user;
    private readonly string _password;

    public ShardConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
        _user = configuration["Database:User"] ?? "sa";
        _password = configuration["Database:Password"]
                    ?? throw new InvalidOperationException("Database:Password must be configured.");
        DatabaseName = configuration["Database:Name"] ?? "ArticleDatabase";
    }

    public string DatabaseName { get; }

    public SqlConnection CreateMasterConnection(string region) =>
        CreateConnection(region, "master");

    public SqlConnection CreateArticleConnection(string region) =>
        CreateConnection(region, DatabaseName);

    private SqlConnection CreateConnection(string region, string database)
    {
        if (!ArticleRegions.TryNormalize(region, out var normalized))
        {
            throw new ArgumentOutOfRangeException(nameof(region), region, "Unknown article region.");
        }

        var configurationName = ArticleRegions.GetConfigurationName(normalized);
        var server = _configuration[$"Shards:{configurationName}"]
                     ?? throw new InvalidOperationException(
                         $"Missing database host configuration for shard '{normalized}'.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = _user,
            Password = _password,
            Encrypt = false,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        };

        return new SqlConnection(builder.ConnectionString);
    }
}
