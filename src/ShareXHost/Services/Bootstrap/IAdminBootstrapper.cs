namespace ShareXHost;

public interface IAdminBootstrapper
{
    Task InitializeAsync();
}

public sealed class AdminBootstrapper : IAdminBootstrapper
{
    private const string BootstrapUserName = "bootstrap_admin";
    
    private readonly IUserService _userService;
    private readonly ILogger<AdminBootstrapper> _logger;

    public AdminBootstrapper(IUserService userService, ILogger<AdminBootstrapper> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        string password = AlphaNumericRandomGenerator.Generate(32);
        
        User? existingAdmin = await _userService.FindByUserNameAsync(BootstrapUserName);
        
        if (existingAdmin != null)
            await _userService.SetPasswordAsync(BootstrapUserName, password);
        else
            await _userService.CreateAsync(BootstrapUserName, password, "App", UserRole.Admin);

        _logger.LogInformation(
            "Bootstrap admin user initialized with username '{BootstrapUserName}' and password '{Password}'",
            BootstrapUserName, password);
    }
}