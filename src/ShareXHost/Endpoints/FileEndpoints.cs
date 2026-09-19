using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ShareXHost;

public static class FileEndpoints
{
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    
    public static IEndpointRouteBuilder MapFileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files/{id:guid}", async (Guid id, IFileService fileService) =>
        {
            GetFileResult result = await fileService.GetAsync(id);
            if (result.IsNotFound) return Results.NotFound();
            return result.HasStorageFailure
                ? Results.InternalServerError()
                : Results.File(result.Stream!, result.ContentType!);
        });
        
        endpoints.MapPost("/files", [RequestSizeLimit(MaxFileSize)] async (IFormFile file, ClaimsPrincipal user,
            IFileService fileService, HttpContext context) =>
        {
            Guid? userId = user.TryGetUserId();
            bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;
            
            await using Stream openReadStream = file.OpenReadStream();
            UploadFileResult result =
                await fileService.UploadAsync(openReadStream, file.Length, file.ContentType, Path.GetFileName(file.FileName), userId);

            return result.Status switch
            {
                UploadFileStatus.Success => Results.Ok(BuildFileUploadResponse(context, result, isAuthenticated)),
                UploadFileStatus.Empty => Results.BadRequest(new { error = "File is empty." }),
                UploadFileStatus.Failure => Results.InternalServerError(new { error = result.Error }),
                _ => Results.InternalServerError(new { error = "Unknown error occurred." })
            };
        }).DisableAntiforgery().RequireRateLimiting("FileLimiter").RequireAuthorization("OptionalAuthentication");
        // Antiforgery is disabled because ShareX does not support getting the CSRF token, and this endpoint is intended for use with ShareX.

        endpoints.MapDelete("/files/{fileId:guid}",
            async (Guid fileId, string? deleteToken, ClaimsPrincipal user, IFileService fileService) =>
        {
            DeleteFileResult result = await fileService.DeleteAsync(
                fileId,
                deleteToken,
                user.TryGetUserId()
            );
            if (result.HasDatabaseFailure) return Results.InternalServerError();
            return result.IsForbidden ? Results.Forbid() : Results.NoContent();
        });
        
        endpoints.MapGet("/files/mine", async (IFileService fileService, ClaimsPrincipal user, int page = 1, int pageSize = 25) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return Results.BadRequest(new { error = "Invalid pagination parameters." });
            Guid? userId = user.TryGetUserId();
            if (userId is null) return Results.Unauthorized();
            PaginatedResponse<FileResponse> files = await fileService.GetFilesForUserAsync(userId.Value, page, pageSize);
            return Results.Ok(files);
        }).RequireAuthorization("JwtOnly");

        return endpoints;
    }
    
    private static UploadResponse BuildFileUploadResponse(HttpContext context, UploadFileResult result, bool isAuthenticated)
    {
        string baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        string fileUrl = $"{baseUrl}/files/{result.FileId}";
        string deletionUrl = isAuthenticated || result.DeleteToken is null
            ? $"{fileUrl}"
            : $"{fileUrl}?deleteToken={result.DeleteToken}";

        return new UploadResponse
        {
            Url = fileUrl,
            DeletionUrl = deletionUrl
        };
    }
}