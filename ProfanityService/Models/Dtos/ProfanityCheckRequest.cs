namespace ProfanityService.Models.Dtos;

/// <summary>
/// The request body for POST api/profanity/check. This is the whole
/// contract CommentService and ProfanityService talk to each other
/// through - see CommentService/Clients/ProfanityServiceClient.cs for
/// the other side of it.
/// </summary>
public class ProfanityCheckRequest
{
    public required string Text { get; set; }
}
