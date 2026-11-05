namespace NewsletterService.Client;

public sealed class SubscriberDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime SubscribedAtUtc { get; set; }
}
