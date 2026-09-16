using System.ComponentModel.DataAnnotations;

namespace ProfanityService.Service.Dto;

public sealed class FilterRequestDto
{
    [Required]
    [MaxLength(5000)]
    public string Text { get; set; } = string.Empty;
}
