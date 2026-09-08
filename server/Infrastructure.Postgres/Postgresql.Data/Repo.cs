using Application.Interfaces.Infrastructure.Postgres;
using Core.Domain.Entities;
using Infrastructure.Postgres.Scaffolding;

namespace Infrastructure.Postgres.Postgresql.Data;

public class Repo(MyDbContext ctx) : IDataRepository
{

}