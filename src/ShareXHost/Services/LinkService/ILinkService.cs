namespace ShareXHost;

public interface ILinkService
{
    Task<UploadLinkResult> CreateAsync(string url, Guid? userId);
    Task<GetLinkResult> GetAsync(string linkId);
    Task<DeleteLinkResult> DeleteAsync(string linkId, string? deleteToken, Guid? userId);
    Task<PaginatedResponse<LinkResponse>> GetLinksForUserAsync(Guid userId, int page, int pageSize);
}