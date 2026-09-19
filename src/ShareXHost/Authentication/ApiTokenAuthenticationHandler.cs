using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ShareXHost;

public sealed class ApiTokenAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiTokenService _apiTokenService;

    public ApiTokenAuthenticationHandler(
        IApiTokenService apiTokenService,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _apiTokenService = apiTokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authHeader))
        {
            if (authHeader.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                string? token = authHeader.Parameter?.Trim();

                if (!string.IsNullOrEmpty(token) && token.StartsWith("shx_", StringComparison.OrdinalIgnoreCase))
                {
                    Guid? userId = await _apiTokenService.AuthenticateAsync(token);

                    if (userId.HasValue)
                    {
                        Claim[] claims = [
                            new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                            new Claim("auth_type", "api_token")
                        ];
                        ClaimsIdentity identity = new(claims, Scheme.Name);
                        ClaimsPrincipal principal = new(identity);
                        AuthenticationTicket ticket = new(principal, Scheme.Name);

                        return AuthenticateResult.Success(ticket);
                    }

                    return AuthenticateResult.Fail("Invalid API Token");
                }
            }
        }

        return AuthenticateResult.NoResult();
    }
}