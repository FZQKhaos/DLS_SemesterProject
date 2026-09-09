using Application.Interfaces.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres;

public class Seeder(MyDbContext context) : ISeeder
{
    public async Task Seed()
    {
        await context.Database.EnsureCreatedAsync();
    }
}