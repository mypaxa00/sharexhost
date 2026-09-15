namespace ShareXHost;

public sealed class AntiForgeryTokenResponse
{
    public string RequestToken { get; init; } = null!;
}