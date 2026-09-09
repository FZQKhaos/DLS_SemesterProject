using Application.Interfaces.Infrastructure.Postgres;

namespace Infrastructure.Postgres.Postgresql.Data;

public class Repo(MyDbContext ctx) : IDataRepository
{

}