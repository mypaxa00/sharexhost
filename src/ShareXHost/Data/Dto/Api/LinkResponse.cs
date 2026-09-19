namespace ShareXHost;

public sealed class LinkResponse
{
    public string ShortId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}