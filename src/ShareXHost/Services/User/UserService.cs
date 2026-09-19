using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ShareXHost;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(AppDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> CreateAsync(
        string userName,
        string password,
        string name,
        UserRole role)
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Name = name,
            Role = role
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User?> AuthenticateAsync(string userName, string password)
    {
        User? result = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == userName);
        
        if (result is null) return null;

        PasswordVerificationResult verificationResult = _passwordHasher.VerifyHashedPassword(result, result.PasswordHash, password);
        return verificationResult != PasswordVerificationResult.Success ? null : result;
    }
    
    public async Task SetPasswordAsync(string userName, string password)
    {
        User? user = await _dbContext.Users.FirstOrDefaultAsync(x => x.UserName == userName);
        if (user is null) throw new InvalidOperationException($"User with username '{userName}' not found.");

        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<User?> FindByUserNameAsync(string userName)
    {
        return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == userName);
    }
}