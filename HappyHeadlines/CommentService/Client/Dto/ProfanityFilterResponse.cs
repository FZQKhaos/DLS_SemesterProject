namespace CommentService.Client.Dto;

public sealed class ProfanityFilterResponse
{
    public string FilteredText { get; set; } = string.Empty;
    public bool ProfanityFound { get; set; }
    public bool ServiceAvailable { get; set; } = true;

    public static ProfanityFilterResponse Unavailable() => new()
    {
        ServiceAvailable = false,
        FilteredText = string.Empty,
        ProfanityFound = false
    };
}
