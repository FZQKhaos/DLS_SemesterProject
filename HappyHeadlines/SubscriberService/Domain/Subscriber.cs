namespace SubscriberService.Domain;

public sealed class Subscriber
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required DateTime SubscribedAtUtc { get; set; }
}
