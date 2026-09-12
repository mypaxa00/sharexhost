namespace ShareXHost;

public sealed class FileDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = null!;
    public long SizeBytes { get; init; }
    public string ContentType { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}