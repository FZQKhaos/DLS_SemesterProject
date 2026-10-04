using ArticleService.Data;
using ArticleService.Data.Interface;
using ArticleService.Domain;
using ArticleService.Cache;
using ArticleService.Service.Dto;
using ArticleService.Service.Interface;
using StackExchange.Redis;

namespace ArticleService.Service;

public class ArticleService(IArticleRepository repo, IArticleCache articleCache, ILogger<ArticleService> logger) : IArticleService
{
    public async Task<ArticleResponseDto> CreateArticle(ArticleRequestDto dto)
    {
        var id = Guid.NewGuid().ToString();
        
        
        var article = new Article()
        {
            Id = id,
            Title = dto.Title,
            Body = dto.Body,
            Continent = dto.Continent,
            PublishedAtUtc = DateTime.UtcNow
        };
        
        var createdArticle = await repo.CreateArticle(article);

        return new ArticleResponseDto
        {
            Id = createdArticle.Id,
            Title = createdArticle.Title,
            Body = createdArticle.Body,
            Continent = createdArticle.Continent
        };
        
    }

    public async Task<ArticleResponseDto> GetArticleByIdAndContinent(string id, string continent)
    {
        // ArticleCache applies only to global articles. Regional articles already live
        // in geographically partitioned databases, while the Week 40 requirement targets
        // repeated reads against the shared global article database.
        if (string.Equals(continent, "global", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Cache hit:
                // Redis contains the global article, so the service can answer without
                // reading the Global ArticleDatabase.
                //
                // Cache miss:
                // Redis was checked successfully but had no value. This service then
                // falls back to the database without writing the article into Redis;
                // ArticleCacheWorker remains responsible for offline cache population.
                var cachedArticle = await articleCache.GetArticleAsync(id);
                if (cachedArticle != null)
                    return MapToResponseDto(cachedArticle);
            }
            catch (RedisException exception)
            {
                // Redis is an optimization layer. If the cache infrastructure cannot be
                // contacted, the database-backed service should still be usable where
                // possible. This is different from a normal miss, where Redis responded
                // successfully and the key was simply absent.
                logger.LogError(exception, "Redis cache read failed for global article {ArticleId}; falling back to database.", id);
            }
        }

        // Regional requests bypass ArticleCache entirely, and global cache misses end up
        // here as a database fallback. Because request-time writeback is intentionally
        // omitted, ArticleCache hit/miss statistics describe only actual global cache
        // lookups performed above.
        var article = await repo.GetArticleByIdAndContinent(id, continent);
        if (article == null)
            throw new InvalidOperationException("Article not found");

        return MapToResponseDto(article);
    }

    private static ArticleResponseDto MapToResponseDto(Article article)
    {
        return new ArticleResponseDto
        {
            Id = article.Id,
            Title = article.Title,
            Body = article.Body,
            Continent = article.Continent
        };
    }

    public async Task<ArticleResponseDto> UpdateArticle(string id, ArticleRequestDto dto)
    {
        var article = await repo.GetArticleByIdAndContinent(id, dto.Continent);
        if (article == null)
            throw new InvalidOperationException("Article not found");

        article.Title = dto.Title;
        article.Body = dto.Body;

        var updatedArticle = await repo.UpdateArticle(id, article);

        return new ArticleResponseDto
        {
            Id = updatedArticle.Id,
            Title = updatedArticle.Title,
            Body = updatedArticle.Body,
            Continent = updatedArticle.Continent
        };
    }

    public async Task<bool> DeleteArticle(string id, string continent)
    {
        var article = await repo.GetArticleByIdAndContinent(id, continent);
        if (article == null)
            throw new InvalidOperationException("Article not found");

        return await repo.DeleteArticle(id, continent);
    }
}
