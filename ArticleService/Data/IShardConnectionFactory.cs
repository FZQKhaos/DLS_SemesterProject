using Microsoft.Data.SqlClient;

namespace ArticleService.Data;

public interface IShardConnectionFactory
{
    string DatabaseName { get; }
    SqlConnection CreateMasterConnection(string region);
    SqlConnection CreateArticleConnection(string region);
}
