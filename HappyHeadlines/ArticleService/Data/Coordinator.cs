using System.Data;
using System.Data.Common;
using Npgsql;

namespace ArticleService.Data;

public class Coordinator()
{
    private IDictionary<string, NpgsqlConnection> connCache = new Dictionary<string, NpgsqlConnection>();
    private const string AFRICA_DB = "africa-db";
    private const string ANTARCTICA_DB = "antarctica-db";
    private const string ASIA_DB = "asia-db";
    private const string EUROPE_DB = "europe-db";
    private const string NORTH_AMERICA_DB = "north-america-db";
    private const string SOUTH_AMERICA_DB = "south-america-db";
    private const string OCEANIA_DB = "oceania-db";
    private const string GLOBAL_DB = "global-db";
    
    public NpgsqlConnection GetConnectionForContinent(string continent)
    {
        var dbName = continent.ToLowerInvariant() switch
        {
            "africa" => AFRICA_DB,
            "antarctica" => ANTARCTICA_DB,
            "asia" => ASIA_DB,
            "europe" => EUROPE_DB,
            "northamerica" or "north_america" => NORTH_AMERICA_DB,
            "southamerica" or "south_america" => SOUTH_AMERICA_DB,
            "oceania" => OCEANIA_DB,
            "global" => GLOBAL_DB,
            _ => throw new InvalidOperationException("Unknown continent")
        };

        return GetConnectionByName(dbName);
    }

    public IEnumerable<DbConnection> GetAllConnections()
    {
        yield return GetConnectionByName(AFRICA_DB);
        yield return GetConnectionByName(ANTARCTICA_DB);
        yield return GetConnectionByName(ASIA_DB);
        yield return GetConnectionByName(EUROPE_DB);
        yield return GetConnectionByName(NORTH_AMERICA_DB);
        yield return GetConnectionByName(SOUTH_AMERICA_DB);
        yield return GetConnectionByName(OCEANIA_DB);
        yield return GetConnectionByName(GLOBAL_DB);
    }

    private NpgsqlConnection GetConnectionByName(string dbName)
    {
        if (connCache.TryGetValue(dbName, out var connection))
            return connection;

        connection = new NpgsqlConnection(
            $"Host={dbName};Database={dbName};Username=appuser;Password=secret123");

        connection.Open();
        connCache[dbName] = connection;
        return connection;
    }
}