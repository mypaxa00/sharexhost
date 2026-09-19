using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ShareXHost;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints, SecurityKey securityKey)
    {
        endpoints.MapGet("/me", (ClaimsPrincipal user) =>
        {
            string name = user.Identity?.Name ?? "Unknown";
            string role = user.FindFirst(ClaimTypes.Role)?.Value ?? "None";
    
            return Results.Ok(new MeResponse()
            {
                Name = name,
                Role = role
            });
        }).RequireAuthorization("JwtOnly");
        
        endpoints.MapPost("/auth/login", async (LoginRequest request, IUserService userService) =>
        {
            User? user = await userService.AuthenticateAsync(request.Name, request.Password);
            if (user is null) return Results.Unauthorized();

            Claim[] claims =
            [
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.Role.ToString())
            ];

            SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: "ShareXHost",
                audience: "ShareXHost",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return Results.Ok(new TokenResponse()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        });
        
        return endpoints;
    }
}