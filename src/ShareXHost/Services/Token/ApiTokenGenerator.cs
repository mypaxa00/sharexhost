using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace ShareXHost;

public sealed class ApiTokenGenerator : IApiTokenGenerator
{
    private const int TokenLength = 32;

    public string Generate()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(TokenLength);
        string encoded = WebEncoders.Base64UrlEncode(bytes);
        return "shx_" + encoded;
    }
}