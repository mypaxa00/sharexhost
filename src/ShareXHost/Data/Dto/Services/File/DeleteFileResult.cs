namespace ShareXHost;

public sealed class DeleteFileResult
{
    public bool IsNoContent { get; private init; }
    public bool IsForbidden { get; private init; }
    public bool HasDatabaseFailure { get; private init; }

    public static DeleteFileResult NoContent() => new()
    {
        IsNoContent = true
    };

    public static DeleteFileResult Forbidden() => new()
    {
        IsForbidden = true
    };

    public static DeleteFileResult DatabaseFailure() => new()
    {
        HasDatabaseFailure = true
    };
}
