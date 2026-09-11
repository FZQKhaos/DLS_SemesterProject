namespace CommentService.Clients.Dtos;

/// <summary>
/// Wire-format DTOs for POST api/profanity/check on ProfanityService.
/// Deliberately a plain copy of ProfanityService's own DTOs rather than a
/// shared library reference - the two services only agree on the JSON
/// shape over HTTP, not on any compiled contract, so either one can
/// change its internals independently as long as this shape stays the same.
/// </summary>
public class ProfanityCheckRequest
{
    public required string Text { get; set; }
}
