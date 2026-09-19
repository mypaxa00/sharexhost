namespace ShareXHost;

public sealed class ApiTokenHasher : IApiTokenHasher
{
    public string Hash(string token)
    {
        byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}