using System.Security.Claims;

namespace ShareXHost;

public static class TokenEndpoints
{
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/tokens", async (ClaimsPrincipal user, IApiTokenService apiTokenService,
            int page = 1, int pageSize = 10) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return Results.BadRequest(new { error = "Invalid pagination parameters." });
            
            Guid? userId = user.TryGetUserId();
            if (userId is null) return Results.Unauthorized();

            PaginatedResponse<GetApiTokenResult> tokens =
                await apiTokenService.GetTokensForUserAsync(userId.Value, page, pageSize);
            return Results.Ok(tokens);
        }).RequireAuthorization("JwtOnly");

        endpoints.MapPost("/auth/tokens", async (IApiTokenService apiTokenService, ClaimsPrincipal user, CreateApiTokenRequest request) =>
        {
            Guid? userId = user.TryGetUserId();
            if (userId is null) return Results.Unauthorized();
            string tokenName = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(tokenName) || tokenName.Length > 100)
                return Results.BadRequest(new { error = "Token name must be between 1 and 100 characters." });
            
            string token = await apiTokenService.CreateAsync(userId.Value, tokenName);
            return Results.Ok(new TokenResponse { Token = token });
        }).RequireAuthorization("JwtOnly");
        
        endpoints.MapDelete("/auth/tokens/{id:guid}", async (Guid id, ClaimsPrincipal user, IApiTokenService apiTokenService) =>
        {
            DeleteTokenResult result = await apiTokenService.DeleteAsync(
                id,
                user.TryGetUserId()
            );
            if (result.HasDatabaseFailure) return Results.InternalServerError();
            return result.IsForbidden ? Results.Forbid() : Results.NoContent();
        }).RequireAuthorization("JwtOnly");
        
        return endpoints;
    }
}