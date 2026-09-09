using ArticleService.Models;
using ArticleService.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Data;

/// <summary>
/// Implements the CRUD operations. Every method first asks the
/// IShardRouter which database the given continent lives in, opens a
/// short-lived ArticleDbContext against exactly that database, and does
/// its work there. No operation ever touches more than one database.
/// </summary>
public class ArticleRepository(IShardRouter shardRouter) : IArticleRepository
{
    public async Task<Article> CreateAsync(Article article)
    {
        await using var db = OpenContext(article.Continent);
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        return article;
    }

    public async Task<Article?> GetAsync(Continent continent, int id)
    {
        await using var db = OpenContext(continent);
        return await db.Articles.FindAsync(id);
    }

    public async Task<Article?> UpdateAsync(Continent continent, int id, UpdateArticleRequest request)
    {
        await using var db = OpenContext(continent);
        var article = await db.Articles.FindAsync(id);
        if (article is null)
        {
            return null;
        }

        article.Title = request.Title;
        article.Content = request.Content;
        article.Author = request.Author;
        await db.SaveChangesAsync();
        return article;
    }

    public async Task<bool> DeleteAsync(Continent continent, int id)
    {
        await using var db = OpenContext(continent);
        var article = await db.Articles.FindAsync(id);
        if (article is null)
        {
            return false;
        }

        db.Articles.Remove(article);
        await db.SaveChangesAsync();
        return true;
    }

    private ArticleDbContext OpenContext(Continent continent)
    {
        var options = new DbContextOptionsBuilder<ArticleDbContext>()
            .UseSqlServer(shardRouter.GetConnectionString(continent))
            .Options;
        return new ArticleDbContext(options);
    }
}
