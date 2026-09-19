using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShareXHost.Storage;
using IPNetwork = System.Net.IPNetwork;

namespace ShareXHost;

// ReSharper disable once PartialTypeWithSinglePart
public partial class Program
{
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        TryLoadSecrets(builder.Configuration);
        
        string jwtSigningKey =
            builder.Configuration.GetValue<string>("JwtSigningKey") ??
            throw new InvalidOperationException("JWT signing key not found.");

        SecurityKey securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSigningKey));

        BuilderConfiguration(builder, securityKey);

        WebApplication app = builder.Build();
        
        app.UseDefaultFiles();
        app.UseStaticFiles();
        
        app.UseForwardedHeaders();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseRateLimiter();
        
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
                await fileService.UploadAsync(openReadStream, file.Length, file.ContentType, Path.GetFileName(file.FileName), userId);

            return result.Status switch
            {
                UploadFileStatus.Success => Results.Ok(BuildFileUploadResponse(context, result, isAuthenticated)),
                UploadFileStatus.Empty => Results.BadRequest(new { error = "File is empty." }),
                UploadFileStatus.Failure => Results.InternalServerError(new { error = result.Error }),
                _ => Results.InternalServerError(new { error = "Unknown error occurred." })
            };
        }).DisableAntiforgery().RequireRateLimiting("FileLimiter").RequireAuthorization("OptionalAuthentication");
        
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
        }).RequireRateLimiting("LinkLimiter").RequireAuthorization("OptionalAuthentication");

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
        
        app.MapGet("/files/mine", async (IFileService fileService, ClaimsPrincipal user, int page = 1, int pageSize = 25) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return Results.BadRequest(new { error = "Invalid pagination parameters." });
            Guid? userId = TryGetUserId(user);
            if (userId is null) return Results.Unauthorized();
            PaginatedResponse<FileResponse> files = await fileService.GetFilesForUserAsync(userId.Value, page, pageSize);
            return Results.Ok(files);
        }).RequireAuthorization("JwtOnly");
        
        app.MapGet("/links/mine", async (ILinkService linkService, ClaimsPrincipal user, int page = 1, int pageSize = 25) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return Results.BadRequest(new { error = "Invalid pagination parameters." });
            Guid? userId = TryGetUserId(user);
            if (userId is null) return Results.Unauthorized();
            PaginatedResponse<LinkResponse> links = await linkService.GetLinksForUserAsync(userId.Value, page, pageSize);
            return Results.Ok(links);
        }).RequireAuthorization("JwtOnly");

        app.MapGet("/me", (ClaimsPrincipal user) =>
        {
            string name = user.Identity?.Name ?? "Unknown";
            string role = user.FindFirst(ClaimTypes.Role)?.Value ?? "None";
    
            return Results.Ok(new MeResponse()
            {
                Name = name,
                Role = role
            });
        }).RequireAuthorization("JwtOnly");
        
        app.MapPost("/auth/login", async (LoginRequest request, IUserService userService) =>
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

        app.MapGet("/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
        {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
            string? token = tokens.RequestToken;
            if (string.IsNullOrEmpty(token)) return Results.InternalServerError();
            return Results.Ok(new AntiForgeryTokenResponse{ RequestToken = token });
        });

        app.MapPost("/admin/users", async (CreateUserRequest request, IUserService userService) =>
        {
            User? existingUser = await userService.FindByUserNameAsync(request.UserName);
            
            if (existingUser != null) return Results.Conflict("User already exists");

            await userService.CreateAsync(request.UserName, request.Password, request.Name, request.Role);
            return Results.Created();
        }).RequireAuthorization("JwtAdmin");

        app.MapGet("/auth/tokens", async (ClaimsPrincipal user, IApiTokenService apiTokenService, int page = 1, int pageSize = 10) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return Results.BadRequest(new { error = "Invalid pagination parameters." });
            
            Guid? userId = TryGetUserId(user);
            if (userId is null) return Results.Unauthorized();

            PaginatedResponse<GetApiTokenResult> tokens =
                await apiTokenService.GetTokensForUserAsync(userId.Value, page, pageSize);
            return Results.Ok(tokens);
        }).RequireAuthorization("JwtOnly");

        app.MapPost("/auth/tokens", async (IApiTokenService apiTokenService, ClaimsPrincipal user, CreateApiTokenRequest request) =>
        {
            Guid? userId = TryGetUserId(user);
            if (userId is null) return Results.Unauthorized();
            string tokenName = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(tokenName) || tokenName.Length > 100)
                return Results.BadRequest(new { error = "Token name must be between 1 and 100 characters." });
            
            string token = await apiTokenService.CreateAsync(userId.Value, tokenName);
            return Results.Ok(new TokenResponse { Token = token });
        }).RequireAuthorization("JwtOnly");
        
        app.MapDelete("/auth/tokens/{id:guid}", async (Guid id, ClaimsPrincipal user, IApiTokenService apiTokenService) =>
        {
            DeleteTokenResult result = await apiTokenService.DeleteAsync(
                id,
                TryGetUserId(user)
            );
            if (result.HasDatabaseFailure) return Results.InternalServerError();
            return result.IsForbidden ? Results.Forbid() : Results.NoContent();
        }).RequireAuthorization("JwtOnly");


        using (IServiceScope scope = app.Services.CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            IAdminBootstrapper bootstrapper =
                scope.ServiceProvider.GetRequiredService<IAdminBootstrapper>();
            await bootstrapper.InitializeAsync();
            
        }
        
        app.Run();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        string? userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(userIdValue) && Guid.TryParse(userIdValue, out Guid userId)
            ? userId
            : null;
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

    private static void TryLoadSecrets(ConfigurationManager builderConfiguration)
    {
        const string jwtSecretPath = "/run/secrets/jwt_key";
        if (System.IO.File.Exists(jwtSecretPath))
        {
            string jwtSigningKey = System.IO.File.ReadAllText(jwtSecretPath).Trim();
            builderConfiguration["JwtSigningKey"] = jwtSigningKey;
        }

        const string postgresPasswordPath = "/run/secrets/postgres_password";
        if (System.IO.File.Exists(postgresPasswordPath))
        {
            string postgresPassword = System.IO.File.ReadAllText(postgresPasswordPath).Trim();

            builderConfiguration["ConnectionStrings:DefaultConnection"] =
                $"Host=postgres;Database=sharexhost;Username=sharexhost;Password={postgresPassword}";
        }
    }


    private static void BuilderConfiguration(WebApplicationBuilder builder,
        SecurityKey securityKey)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            
            string[] knownNetworks =
                builder.Configuration
                    .GetSection("ForwardedHeaders:KnownNetworks")
                    .Get<string[]>() ?? [];

            foreach (string network in knownNetworks)
            {
                options.KnownIPNetworks.Add(IPNetwork.Parse(network));
            }
        });
        
        string connectionString =
            builder.Configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
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
        builder.Services.AddSingleton<IApiTokenGenerator, ApiTokenGenerator>();
        builder.Services.AddSingleton<IApiTokenHasher, ApiTokenHasher>();
        builder.Services.AddScoped<IApiTokenService, ApiTokenService>();

        builder.Services
            .AddAuthentication("Bearer")
            .AddPolicyScheme("Bearer", null, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string? authorization = context.Request.Headers.Authorization;

                    return authorization?.StartsWith("Bearer shx_", StringComparison.OrdinalIgnoreCase) == true 
                        ? "ApiToken" 
                        : "JwtBearer";
                };
            })
            .AddJwtBearer("JwtBearer", options =>
            {
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidIssuer = "ShareXHost";

                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = "ShareXHost";

                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.IssuerSigningKey = securityKey;
            })
            .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(
                "ApiToken",
                _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("JwtOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.FindFirst("auth_type")?.Value != "api_token");
            });
            
            options.AddPolicy("JwtAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(nameof(UserRole.Admin));
                policy.RequireAssertion(context =>
                    context.User.FindFirst("auth_type")?.Value != "api_token");
            });
            
            options.AddPolicy("OptionalAuthentication", policy =>
            {
                policy.AddAuthenticationSchemes("Bearer");

                policy.RequireAssertion(context =>
                {
                    bool hasAuthHeader = context.Resource is HttpContext httpContext &&
                                         httpContext.Request.Headers.ContainsKey("Authorization");

                    return !hasAuthHeader || context.User.Identity?.IsAuthenticated == true;
                });
            });
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
        });
        
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            bool enabled = builder.Configuration.GetValue<bool>("RateLimiting:Enabled");

            if (enabled)
            {
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
            }
            else
            {
                options.AddPolicy("LinkLimiter", _ => RateLimitPartition.GetNoLimiter("testing"));
                options.AddPolicy("FileLimiter", _ => RateLimitPartition.GetNoLimiter("testing"));
            }
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IAdminBootstrapper, AdminBootstrapper>();
    }
}