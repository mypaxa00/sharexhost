namespace ShareXHost;

public interface IUserService
{
    Task<User> CreateAsync(
        string userName,
        string password,
        string name,
        UserRole role);
    
    Task<User?> AuthenticateAsync(string userName, string password);
    
    Task SetPasswordAsync(string userName, string password);
    
    Task<User?> FindByUserNameAsync(string userName);
}