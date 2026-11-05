namespace Events;

/// <summary>
/// Message SubscriberService puts in SubscriberQueue when someone subscribes.
/// NewsletterService listens for it and sends a welcome mail.
/// </summary>
public sealed class SubscriberSubscribed
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required DateTime SubscribedAtUtc { get; set; }
}
