using System.Security.Claims;

namespace ShareXHost;

public static class ClaimsPrincipalExtensions
{
    extension (ClaimsPrincipal user)
    {
        public Guid? TryGetUserId()
        {
            string? userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userIdValue) && Guid.TryParse(userIdValue, out Guid userId)
                ? userId
                : null;
        }
    }
}