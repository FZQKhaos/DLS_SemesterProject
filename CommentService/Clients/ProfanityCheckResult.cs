namespace CommentService.Clients;

/// <summary>
/// The result CommentService actually reasons about internally. It's
/// deliberately not the same shape as ProfanityService's HTTP response:
/// <see cref="Checked"/> is CommentService's own concept of "did we
/// actually manage to ask ProfanityService", which doesn't exist on the
/// wire at all - it's set by ProfanityServiceClient's fallback path when
/// the call couldn't be made.
/// </summary>
public record ProfanityCheckResult(bool IsProfane, bool Checked);
