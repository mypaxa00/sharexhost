using Microsoft.EntityFrameworkCore;

namespace ShareXHost;

public sealed class ApiTokenService : IApiTokenService
{
    private readonly AppDbContext _dbContext;
    private readonly IApiTokenGenerator _tokenGenerator;
    private readonly IApiTokenHasher _tokenHasher;
    
    public ApiTokenService(AppDbContext dbContext, IApiTokenGenerator tokenGenerator, IApiTokenHasher tokenHasher)
    {
        _dbContext = dbContext;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
    }
    
    public async Task<string> CreateAsync(Guid userId, string name)
    {
        string token = _tokenGenerator.Generate();
        string hashedToken = _tokenHasher.Hash(token);

        ApiToken apiToken = new ApiToken
        {
            UserId = userId,
            Name = name,
            TokenHash = hashedToken,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ApiTokens.Add(apiToken);
        await _dbContext.SaveChangesAsync();

        return token;
    }

    public Task<Guid?> AuthenticateAsync(string token)
    {
        string hashedToken = _tokenHasher.Hash(token);
        return _dbContext.ApiTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == hashedToken)
            .Select(t => (Guid?)t.UserId)
            .FirstOrDefaultAsync();
    }

    public async Task<PaginatedResponse<GetApiTokenResult>> GetTokensForUserAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0 and less than or equal to 100.");
        
        List<GetApiTokenResult> list = await _dbContext.ApiTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new GetApiTokenResult
            {
                Id = t.Id,
                Name = t.Name,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
        
        int totalCount = await _dbContext.ApiTokens.AsNoTracking()
            .Where(t => t.UserId == userId)
            .CountAsync();
        
        return new PaginatedResponse<GetApiTokenResult>(
            list,
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<DeleteTokenResult> DeleteAsync(Guid id, Guid? userId)
    {
        ApiToken? token = await _dbContext.ApiTokens.FirstOrDefaultAsync(t => t.Id == id);
        if (token is null) return DeleteTokenResult.NoContent();

        if (userId.HasValue && token.UserId == userId)
        {
            _dbContext.ApiTokens.Remove(token);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return DeleteTokenResult.DataBaseFailure();
            }
            return DeleteTokenResult.Success();
        }

        return DeleteTokenResult.Forbidden();
    }
}