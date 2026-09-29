namespace Events;

public sealed class ArticlePublished
{
    public Dictionary<string, object> Header { get; set; } = new();
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public required string Continent { get; set; }
}
