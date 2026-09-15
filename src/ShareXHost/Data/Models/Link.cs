namespace ShareXHost;

public sealed class Link
{
    public Guid? UserId { get; set; }
    public string ShortId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? DeleteToken { get; set; }

    public User? User { get; set; }
}