namespace ShareXHost;

public sealed class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public ICollection<ApiToken> ApiTokens { get; set; } = [];
}

public enum UserRole : byte
{
    User = 0,
    Admin = 255
}
