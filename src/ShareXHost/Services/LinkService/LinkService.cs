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

    public async Task<Link> CreateAsync(string url, Guid? userId)
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
                if (attempts == maxAttempts) throw;
            }
        }

        return link;

        async Task<Link> GenerateAndStoreLink()
        {
            Link link1 = new()
            {
                Id = Guid.NewGuid(),
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
}