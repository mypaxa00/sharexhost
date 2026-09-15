namespace ShareXHost;

public class GetLinkResult
{
    public string? Url { get; private init; }

    public static GetLinkResult SuccessResult(string url) =>
        new()
        {
            Url = url
        };

    public static GetLinkResult NotFound() =>
        new()
        {
            Url = null
        };
}