using System.ComponentModel.DataAnnotations;

namespace SubscriberService.Service.Dto;

public sealed class SubscribeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
