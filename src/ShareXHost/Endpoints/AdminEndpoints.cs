namespace ShareXHost;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/admin/users", async (CreateUserRequest request, IUserService userService) =>
        {
            User? existingUser = await userService.FindByUserNameAsync(request.UserName);
            
            if (existingUser is not null) return Results.Conflict("User already exists");

            await userService.CreateAsync(request.UserName, request.Password, request.Name, request.Role);
            return Results.Created();
        }).RequireAuthorization("JwtAdmin");
        
        return endpoints;
    }
}