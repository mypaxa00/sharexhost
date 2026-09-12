using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace ShareXHost;

public class DeleteFileHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, File>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        File resource)
    {
        if(requirement.Name != Operations.Delete.Name) return Task.CompletedTask;
        
        string? userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdString, out Guid userId) && resource.UserId == userId)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}