using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ShareXHost;

public sealed class CustomAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public CustomAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string token = Request.Headers["Authorization"].ToString();
        if (!token.StartsWith("Custom ")) return Task.FromResult(AuthenticateResult.NoResult());
        int space = token.IndexOf(' ');
        string name = token[(space + 1)..];
        string? guid = GuidByName(name);
        if (guid is null) return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        ClaimsIdentity identity = new([
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.NameIdentifier, guid)
        ], "Custom");
        ClaimsPrincipal principal = new(identity);
    
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
    
    private string? GuidByName(string name)
    {
        // For demonstration purposes, we return a fixed GUID for any name.
        // In a real application, you would look up the user in a database or other data store.
        switch (name)
        {
            case "Alice":
                return "550e8400-e29b-41d4-a716-446655440000";
            case "Bob":
                return "11111111-1111-1111-1111-111111111111";
            default:
                return null;
        }
    }
}