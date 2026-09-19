namespace ShareXHost;

public sealed class DeleteTokenResult
{
    public bool IsNoContent { get; private init; }
    public bool IsForbidden { get; private init; }
    public bool HasDatabaseFailure { get; private init; }

    public static DeleteTokenResult Success() => new();
    
    public static DeleteTokenResult NoContent() => new()
    {
        IsNoContent = true
    };

    public static DeleteTokenResult Forbidden() => new()
    {
        IsForbidden = true
    };

    public static DeleteTokenResult DatabaseFailure() => new()
    {
        HasDatabaseFailure = true
    };
}