namespace SubscriberService.Service.Dto;

public sealed record SubscriberResponseDto(string Id, string Email, DateTime SubscribedAtUtc);
