namespace ShareXHost;

public sealed class GetApiTokenResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}