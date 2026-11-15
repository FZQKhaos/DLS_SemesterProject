namespace Events;

// Shared notification contract for the dedicated subscriber message broker.
public sealed class SubscriberNotification
{
    public required string NotificationId { get; set; }
    public required string Email { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public required string Kind { get; set; } // welcome | article
}
