using System.ComponentModel.DataAnnotations;
using System.Text;
using Application.Interfaces;
using Application.Interfaces.Infrastructure.Postgres;
using Application.Models;
using Application.Models.Dtos;
using Application.Models.Dtos.Article;
using Application.Models.Enums;
using Core.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class ArticleService(IOptionsMonitor<AppOptions> optionsMonitor, IDataRepository repository) : IArticleService
{
    public Task<ArticleResponseDto> GetArticlesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ArticleResponseDto> CreateArticleAsync(ArticleRequestDto article)
    {
        throw new NotImplementedException();
    }

    public Task<ArticleResponseDto> UpdateArticleAsync(ArticleRequestDto article)
    {
        throw new NotImplementedException();
    }

    public Task<ArticleResponseDto> DeleteArticleAsync(int id)
    {
        throw new NotImplementedException();
    }
}