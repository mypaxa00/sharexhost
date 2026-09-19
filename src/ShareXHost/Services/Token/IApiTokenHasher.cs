namespace ShareXHost;

public interface IApiTokenHasher
{
    string Hash(string token);
}