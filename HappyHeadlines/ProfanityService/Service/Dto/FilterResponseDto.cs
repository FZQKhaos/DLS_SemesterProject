namespace ProfanityService.Service.Dto;

public sealed class FilterResponseDto
{
    public string FilteredText { get; set; } = string.Empty;
    public bool ProfanityFound { get; set; }
    public bool ServiceAvailable { get; set; } = true;
}
