namespace ShareXHost;

public sealed class LoggedAdminBootstrapper : IAdminBootstrapper
{
    private const string BootstrapUserName = "bootstrap-admin";
    private const int PasswordLength = 32;
    
    private readonly IUserService _userService;
    private readonly ILogger<LoggedAdminBootstrapper> _logger;

    public LoggedAdminBootstrapper(IUserService userService, ILogger<LoggedAdminBootstrapper> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        string password = AlphaNumericRandomGenerator.Generate(PasswordLength);  // 190 bits of entropy
        
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