using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShareXHost.Storage;

namespace ShareXHost;

// ReSharper disable once PartialTypeWithSinglePart
public partial class Program
{
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string connectionString =
            builder.Configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        string jwtSigningKey =
            builder.Configuration.GetValue<string>("JwtSigningKey") ??
            throw new InvalidOperationException("JWT signing key not found.");

        SecurityKey securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSigningKey));

        BuilderConfiguration(builder, connectionString, securityKey);

        WebApplication app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseRateLimiter();

        app.MapGet("/", () => "Hello World!");

        app.MapGet("/files/{id:guid}", async (Guid id, IFileService fileService) =>
        {
            GetFileResult result = await fileService.GetAsync(id);
            if (result.IsNotFound) return Results.NotFound();
            return result.HasStorageFailure
                ? Results.InternalServerError()
                : Results.File(result.Stream!, result.ContentType!);
        });
        
        app.MapGet("/s/{linkId}", async (string linkId, ILinkService linkService) =>
        {
            GetLinkResult result = await linkService.GetAsync(linkId);
            return result.Url is null ? Results.NotFound() : Results.Redirect(result.Url!);
        });

        app.MapPost("/files", [RequestSizeLimit(MaxFileSize)] async (IFormFile file, ClaimsPrincipal user,
            IFileService fileService, HttpContext context) =>
        {
            Guid? userId = TryGetUserId(user);
            bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;
            
            await using Stream openReadStream = file.OpenReadStream();
            UploadFileResult result =
                await fileService.UploadAsync(openReadStream, file.Length, file.ContentType, userId);

            return result.Status switch
            {
                UploadFileStatus.Success => Results.Ok(BuildFileUploadResponse(context, result, isAuthenticated)),
                UploadFileStatus.Empty => Results.BadRequest(new { error = "File is empty." }),
                UploadFileStatus.Failure => Results.InternalServerError(new { error = result.Error }),
                _ => Results.InternalServerError(new { error = "Unknown error occurred." })
            };
        }).DisableAntiforgery().RequireRateLimiting("FileLimiter");
        
        app.MapPost("/links", async (CreateLinkRequest linkRequest, ClaimsPrincipal user,
            ILinkService linkService, HttpContext context) =>
        {
            Guid? userId = TryGetUserId(user);
            bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;
            
            if (string.IsNullOrEmpty(linkRequest.Url)) return Results.BadRequest(new { error = "No URL provided." });
            
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
        }).RequireRateLimiting("LinkLimiter");

        app.MapDelete("/files/{fileId:guid}",
            async (Guid fileId, string? deleteToken, ClaimsPrincipal user, IFileService fileService) =>
        {
            DeleteFileResult result = await fileService.DeleteAsync(
                fileId,
                deleteToken,
                TryGetUserId(user)
            );
            if (result.HasDatabaseFailure) return Results.InternalServerError();
            return result.IsForbidden ? Results.Forbid() : Results.NoContent();
        });
        
        app.MapDelete("/links/{linkId}",
            async (string linkId, string? deleteToken, ClaimsPrincipal user, ILinkService linkService) =>
        {
            DeleteLinkResult result = await linkService.DeleteAsync(
                linkId,
                deleteToken,
                TryGetUserId(user)
            );
            if (result.HasDatabaseFailure) return Results.InternalServerError();
            return result.IsForbidden ? Results.Forbid() : Results.NoContent();
        });

        app.MapGet("/me", (ClaimsPrincipal user) =>
        {
            string name = user.Identity?.Name ?? "Unknown";
            string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string issuer = user.FindFirst(JwtRegisteredClaimNames.Iss)?.Value ?? "Unknown";
            string audience = user.FindFirst(JwtRegisteredClaimNames.Aud)?.Value ?? "Unknown";
            string role = user.FindFirst(ClaimTypes.Role)?.Value ?? "None";
    
            return Results.Ok(new
            {
                name,
                userId,
                issuer,
                audience,
                role
            });
        }).RequireAuthorization();

        app.MapGet("/dev-token/{name}", (string name) => 
        {
            string? guidByName = GuidByName();
            
            if (guidByName is null)
            {
                return Results.BadRequest(new { error = "Invalid name provided." });
            }
            
            Claim[] claims =
            [
                new(ClaimTypes.Name, name),
                new(ClaimTypes.NameIdentifier, guidByName),
            ];
    
            SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: "ShareXHost",
                audience: "ShareXHost",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return Results.Ok(new JwtTokenResponse()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            });
    
            string? GuidByName()
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
        });

        app.MapGet("/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
        {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
            string? token = tokens.RequestToken;
            if (string.IsNullOrEmpty(token)) return Results.InternalServerError();
            return Results.Ok(new AntiForgeryTokenResponse{ RequestToken = token });
        });

        app.Run();

        static Guid? TryGetUserId(ClaimsPrincipal user)
        {
            string? userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userIdValue) && Guid.TryParse(userIdValue, out Guid userId)
                ? userId
                : null;
        }

        UploadResponse BuildFileUploadResponse(HttpContext context, UploadFileResult result, bool isAuthenticated)
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
        
        UploadResponse BuildLinkUploadResponse(HttpContext context, UploadLinkResult result, bool isAuthenticated)
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


    private static void BuilderConfiguration(WebApplicationBuilder builder, string connectionString,
        SecurityKey securityKey)
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.LogTo(Console.WriteLine);
            options.EnableDetailedErrors();
        });
        builder.Services.AddSingleton<IAuthorizationHandler, DeleteFileHandler>();
        builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
        builder.Services.AddSingleton<IShortIdGenerator, ShortIdGenerator>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<ILinkService, LinkService>();

        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidIssuer = "ShareXHost";
        
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = "ShareXHost";
        
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.IssuerSigningKey = securityKey;
            });

        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
        });
        
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            options.AddPolicy("LinkLimiter", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
            
            options.AddPolicy("FileLimiter", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });
    }
}