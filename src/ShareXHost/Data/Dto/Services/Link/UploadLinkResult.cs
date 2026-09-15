namespace ShareXHost;

public class UploadLinkResult
{
    public UploadLinkStatus Status { get; private init; }
    public string? ShortId { get; private init; }
    public string? DeleteToken { get; private init; }
    public string? Error { get; private init; }

    public static UploadLinkResult Success(string shortId, string? deleteToken) =>
        new()
        {
            Status = UploadLinkStatus.Success,
            ShortId = shortId,
            DeleteToken = deleteToken
        };
    
    public static UploadLinkResult Failure(string error) =>
        new()
        {
            Status = UploadLinkStatus.Failure,
            Error = error
        };
}