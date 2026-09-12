namespace ShareXHost;

public interface ILinkService
{
    Task<Link> CreateAsync(string url, Guid? userId);
}