namespace ShareXHost;

public class DeleteLinkResult
{
    public bool IsForbidden { get; private init; }
    public bool HasDatabaseFailure { get; private init; }
    public bool IsNoContent { get; private init; }

    public static DeleteLinkResult Success() =>
        new()
        {
            IsForbidden = false,
            HasDatabaseFailure = false,
            IsNoContent = false
        };

    public static DeleteLinkResult Forbidden() =>
        new()
        {
            IsForbidden = true,
            HasDatabaseFailure = false,
            IsNoContent = false
        };

    public static DeleteLinkResult NoContent() =>
        new()
        {
            IsForbidden = false,
            HasDatabaseFailure = false,
            IsNoContent = true
        };

    public static DeleteLinkResult DatabaseFailure() =>
        new()
        {
            IsForbidden = false,
            HasDatabaseFailure = true,
            IsNoContent = false
        };
}