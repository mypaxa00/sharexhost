using System.Security.Cryptography;

namespace ShareXHost;

public static class AlphaNumericRandomGenerator
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    
    public static string Generate(int length = 6)
    {
        if (length < 1) throw new ArgumentException("Length must be greater than 0.", nameof(length));
        int toExclusive = Chars.Length;
        return new string(Enumerable.Range(0, length).Select(_ => Chars[RandomNumberGenerator.GetInt32(toExclusive)]).ToArray());
    }
}