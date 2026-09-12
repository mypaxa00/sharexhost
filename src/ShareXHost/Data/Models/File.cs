namespace ShareXHost;

public sealed class File
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string StoragePath { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? DeleteToken { get; set; }

    public User? User { get; set; }
}