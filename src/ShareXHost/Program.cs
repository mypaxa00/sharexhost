using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShareXHost.Storage;
using IPNetwork = System.Net.IPNetwork;

namespace ShareXHost;

// ReSharper disable once PartialTypeWithSinglePart
public partial class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        TryLoadSecrets(builder.Configuration);
        
        string jwtSigningKey =
            builder.Configuration.GetValue<string>("JwtSigningKey") ??
            throw new InvalidOperationException("JWT signing key not found.");

        SecurityKey securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSigningKey));

        ConfigureServices(builder, securityKey);

        WebApplication app = builder.Build();
        
        app.UseDefaultFiles();
        app.UseStaticFiles();
        
        app.UseForwardedHeaders();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapFileEndpoints();
        app.MapLinkEndpoints();
        app.MapTokenEndpoints();
        app.MapAuthEndpoints(securityKey);
        app.MapAdminEndpoints();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            IAdminBootstrapper bootstrapper = scope.ServiceProvider.GetRequiredService<IAdminBootstrapper>();
            await bootstrapper.InitializeAsync();
        }
        
        app.Run();
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


    private static void ConfigureServices(WebApplicationBuilder builder,
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
            if (builder.Environment.IsDevelopment())
            {
                options.LogTo(Console.WriteLine);
                options.EnableDetailedErrors();
            }
        });
        builder.Services.AddSingleton<IAuthorizationHandler, DeleteFileHandler>();
        builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
        builder.Services.AddSingleton<IShortIdGenerator, ShortIdGenerator>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<ILinkService, LinkService>();
        builder.Services.AddSingleton<IApiTokenGenerator, ApiTokenGenerator>();
        builder.Services.AddSingleton<IApiTokenHasher, ApiTokenHasher>();
        builder.Services.AddScoped<IApiTokenService, ApiTokenService>();

        builder.Services.AddAuthentication("Bearer")
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
                options.TokenValidationParameters.ValidateLifetime = true;
                
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

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("JwtOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.FindFirst("auth_type")?.Value != "api_token");
            })
            .AddPolicy("JwtAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(nameof(UserRole.Admin));
                policy.RequireAssertion(context =>
                    context.User.FindFirst("auth_type")?.Value != "api_token");
            })
            .AddPolicy("OptionalAuthentication", policy =>
            {
                policy.AddAuthenticationSchemes("Bearer");

                policy.RequireAssertion(context =>
                {
                    bool hasAuthHeader = context.Resource is HttpContext httpContext &&
                                         httpContext.Request.Headers.ContainsKey("Authorization");

                    return !hasAuthHeader || context.User.Identity?.IsAuthenticated == true;
                });
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
        builder.Services.AddScoped<IAdminBootstrapper, LoggedAdminBootstrapper>();
    }
}