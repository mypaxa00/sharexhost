using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShareXHost;
using ShareXHost.Storage;
using File = ShareXHost.File;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
string jwtSigningKey =
    builder.Configuration.GetValue<string>("JwtSigningKey") ??
    throw new InvalidOperationException("JWT signing key not found.");

SecurityKey securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSigningKey));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.LogTo(Console.WriteLine);
    options.EnableDetailedErrors();
});
builder.Services.AddSingleton<IAuthorizationHandler, DeleteFileHandler>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

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

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => "Hello World!");

app.MapGet("/files/{id}", async (Guid id, AppDbContext db, IFileStorage storage) =>
{
    var file = await db.Files
        .AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new
        {
            x.StoragePath,
            x.ContentType
        })
        .FirstOrDefaultAsync();
    
    if (file is null) return Results.NotFound();
    
    Stream? stream = await storage.GetFileAsync(file.StoragePath);

    return stream is null
        ? Results.InternalServerError()
        : Results.File(stream, file.ContentType);
});

app.MapPost("/files", async (IFormFile file, ClaimsPrincipal user, IFileStorage storage, AppDbContext db, ILogger<Program> logger, HttpContext context) =>
{
    if (file.Length == 0) return Results.BadRequest(new { error = "File is empty." });

    string storagePath;
    await using (Stream openReadStream = file.OpenReadStream())
    {
        storagePath = await storage.SaveFileAsync(openReadStream);
    }

    Guid? userId = null;
    string? userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out Guid id))
        userId = id;
    
    File newFile = new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        StoragePath = storagePath,
        SizeBytes = file.Length,
        ContentType = file.ContentType,
        CreatedAt = DateTime.UtcNow,
        DeleteToken = Guid.NewGuid().ToString()
    };

    db.Files.Add(newFile);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (Exception e)
    {
        logger.LogError(e, "Error saving new file to the database.");

        _ = storage.DeleteFileAsync(storagePath)
            .ContinueWith(
                task => logger.LogError(task.Exception,
                    "Error deleting file from storage after database save failure." +
                    "Storage path: {StoragePath}. Orphan file may remain in storage.", storagePath),
                TaskContinuationOptions.OnlyOnFaulted
            );
        
        return Results.InternalServerError(new { error = "Error saving new file to the database." });
    }

    string baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    
    return Results.Ok(new
    {
        url = $"{baseUrl}/files/{newFile.Id}",
        deletionUrl = $"{baseUrl}/files/{newFile.Id}{(user.Identity?.IsAuthenticated ?? false ? "" : $"?deleteToken={newFile.DeleteToken}")}"
    });
});

app.MapDelete("/files/{fileId}",
    async (Guid fileId, string? deleteToken, ClaimsPrincipal user, AppDbContext db, IFileStorage storage, IAuthorizationService authorization, ILogger<Program> logger) =>
{
    File? file = await db.Files.FirstOrDefaultAsync(x => x.Id == fileId);
    if (file is null) return Results.NoContent();
    
    AuthorizationResult result = await authorization.AuthorizeAsync(user, file, Operations.Delete);
    if ((file.UserId is null && file.DeleteToken == deleteToken) || result.Succeeded)
    {
        db.Files.Remove(file);
        try
        {
            await db.SaveChangesAsync();
            await storage.DeleteFileAsync(file.StoragePath);
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Error deleting database entry for {FileId}", file.Id);
            return Results.InternalServerError();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unexpected error deleting file {FileId}. Orphan file may remain in storage.", file.Id);
        }
        
        return Results.NoContent();
    }
    
    logger.LogWarning("Unauthorized delete attempt for file {FileId} by user {UserId}", file.Id, user.Identity?.Name);
    return user.Identity?.IsAuthenticated != true ? Results.NoContent() : Results.Forbid();
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
    Claim[] claims =
    [
        new(ClaimTypes.Name, name),
        new(ClaimTypes.NameIdentifier, GuidByName() ?? Guid.NewGuid().ToString()),
    ];
    
    SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

    JwtSecurityToken token = new(
        issuer: "ShareXHost",
        audience: "ShareXHost",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token)
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
    return Results.Ok(new { requestToken = tokens.RequestToken });
});

app.Run();



public partial class Program { }