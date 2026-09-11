namespace ProfanityService.Models.Dtos;

/// <summary>
/// The response body for POST api/profanity/check.
/// </summary>
public class ProfanityCheckResponse
{
    public bool IsProfane { get; set; }
    public List<string> MatchedWords { get; set; } = [];
}
