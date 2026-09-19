namespace ShareXHost;

public interface IApiTokenService
{
    Task<string> CreateAsync(Guid userId, string name);
    Task<Guid?> AuthenticateAsync(string token);
    Task<PaginatedResponse<GetApiTokenResult>> GetTokensForUserAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<DeleteTokenResult> DeleteAsync(Guid id, Guid? userId);
}