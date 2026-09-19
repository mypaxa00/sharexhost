using System.Security.Claims;

namespace ShareXHost;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/s/{linkId}", async (string linkId, ILinkService linkService) =>
        {
            GetLinkResult result = await linkService.GetAsync(linkId);
            return result.Url is null ? Results.NotFound() : Results.Redirect(result.Url!);
        });
        
        endpoints.MapPost("/links", async (CreateLinkRequest linkRequest, ClaimsPrincipal user,
            ILinkService linkService, HttpContext context) =>
        {
            Guid? userId = user.TryGetUserId();
            bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;
            
            if (string.IsNullOrWhiteSpace(linkRequest.Url)) return Results.BadRequest(new { error = "No URL provided." });
            
            // verify that the URL is valid
            if (!Uri.TryCreate(linkRequest.Url, UriKind.Absolute, out Uri? uriResult) || 
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return Results.BadRequest(new { error = "Invalid URL provided." });
            }
            
            UploadLinkResult result = await linkService.CreateAsync(linkRequest.Url, userId);
            return result.Status switch
            {
                UploadLinkStatus.Success => Results.Ok(BuildLinkUploadResponse(context, result, isAuthenticated)),
                UploadLinkStatus.Failure => Results.InternalServerError(new { error = result.Error }),
                _ => Results.InternalServerError(new { error = "Unknown error occurred." })
            };
        }).RequireRateLimiting("LinkLimiter").RequireAuthorization("OptionalAuthentication");

        endpoints.MapDelete("/links/{linkId}",
            async (string linkId, string? deleteToken, ClaimsPrincipal user, ILinkService linkService) =>
        {
            DeleteLinkResult result = await linkService.DeleteAsync(
                linkId,
                deleteToken,
                user.TryGetUserId()
            );
            if (result.HasDatabaseFailure) return Results.InternalServerError();
            return result.IsForbidden ? Results.Forbid() : Results.NoContent();
        });

        endpoints.MapGet("/links/mine", async (ILinkService linkService, ClaimsPrincipal user, int page = 1, int pageSize = 25) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return Results.BadRequest(new { error = "Invalid pagination parameters." });
            Guid? userId = user.TryGetUserId();
            if (userId is null) return Results.Unauthorized();
            PaginatedResponse<LinkResponse> links = await linkService.GetLinksForUserAsync(userId.Value, page, pageSize);
            return Results.Ok(links);
        }).RequireAuthorization("JwtOnly");

        return endpoints;
    }
    
    private static UploadResponse BuildLinkUploadResponse(HttpContext context, UploadLinkResult result, bool isAuthenticated)
    {
        string baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        string linkUrl = $"{baseUrl}/s/{result.ShortId}";
        string deletionUrl = isAuthenticated || result.DeleteToken is null
            ? $"{baseUrl}/links/{result.ShortId}"
            : $"{baseUrl}/links/{result.ShortId}?deleteToken={result.DeleteToken}";

        return new UploadResponse
        {
            Url = linkUrl,
            DeletionUrl = deletionUrl
        };
    }
}