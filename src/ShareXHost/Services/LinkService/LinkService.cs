using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ShareXHost;

public sealed class LinkService : ILinkService
{
    private readonly IShortIdGenerator _shortIdGenerator;
    private readonly AppDbContext _dbContext;
    
    public LinkService(IShortIdGenerator shortIdGenerator, AppDbContext dbContext)
    {
        _shortIdGenerator = shortIdGenerator;
        _dbContext = dbContext;
    }

    public async Task<UploadLinkResult> CreateAsync(string url, Guid? userId)
    {
        Link link;
        
        const int maxAttempts = 5;
        int attempts = 0;
        while (true)
        {
            try
            {
                link = await GenerateAndStoreLink();
                break;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                attempts++;
                if (attempts == maxAttempts)
                {
                    return UploadLinkResult.Failure($"Failed to generate a unique short link after {maxAttempts} attempts.");
                }
            }
        }

        return UploadLinkResult.Success(link.ShortId, userId.HasValue ? null : link.DeleteToken);

        async Task<Link> GenerateAndStoreLink()
        {
            Link link1 = new()
            {
                ShortId = _shortIdGenerator.Generate(),
                Url = url,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                DeleteToken = Guid.NewGuid().ToString()
            };

            _dbContext.Links.Add(link1);
            
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch
            {
                _dbContext.Links.Remove(link1);
                throw;
            }
            
            return link1;
        }
    }

    public async Task<GetLinkResult> GetAsync(string linkId)
    {
        Link? link = await _dbContext.Links.FirstOrDefaultAsync(x => x.ShortId == linkId);
        if (link is null) return GetLinkResult.NotFound();
        
        return GetLinkResult.SuccessResult(link.Url);
    }

    public async Task<DeleteLinkResult> DeleteAsync(string linkId, string? deleteToken, Guid? userId)
    {
        Link? link = await _dbContext.Links.FirstOrDefaultAsync(x => x.ShortId == linkId);
        if (link is null) return DeleteLinkResult.NoContent();
        
        bool canDeleteOwnedLink = userId.HasValue && link.UserId == userId;
        bool canDeleteGuestLink = link.UserId is null && link.DeleteToken == deleteToken;
        
        if (canDeleteGuestLink || canDeleteOwnedLink)
        {
            _dbContext.Links.Remove(link);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                return DeleteLinkResult.Failure("Error deleting the link from the database.");
            }
            
            return DeleteLinkResult.Success();
        }

        return DeleteLinkResult.Forbidden();
    }
}

public enum UploadLinkStatus
{
    Success = 0,
    Failure
}