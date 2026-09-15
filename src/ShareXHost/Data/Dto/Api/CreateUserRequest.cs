namespace ShareXHost;

public sealed class CreateUserRequest
{
    public string UserName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Password { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.User;
}