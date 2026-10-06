using PublisherService.Domain;
using PublisherService.Service.Dto;

namespace PublisherService.Service;

public sealed class PublisherService(ArticleQueuePublisher articleQueuePublisher)
{
    public Task PublishAsync(PublishArticleRequestDto request)
    {
        var article = new Article
        {
            Id = request.Id,
            Title = request.Title,
            Body = request.Body,
            Continent = request.Continent
        };

        return articleQueuePublisher.PublishAsync(article);
    }
}