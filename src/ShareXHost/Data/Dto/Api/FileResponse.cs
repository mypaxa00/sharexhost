namespace ShareXHost;

public sealed class FileResponse
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}