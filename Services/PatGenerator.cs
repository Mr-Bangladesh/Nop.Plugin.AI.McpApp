using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.AI.McpApp.Services;

public static class PatGenerator
{
    private const string PREFIX = "nop_pat_";

    public static (string RawToken, string Hash, string DisplayPrefix) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = PREFIX + Convert.ToBase64String(bytes)
            .Replace("+", "").Replace("/", "").Replace("=", "");
        var hash = ComputeHash(raw);
        var displayPrefix = raw[..Math.Min(raw.Length, PREFIX.Length + 8)];
        return (raw, hash, displayPrefix);
    }

    public static string ComputeHash(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    public static bool LooksLikePat(string rawToken)
    {
        return !string.IsNullOrEmpty(rawToken) && rawToken.StartsWith(PREFIX, StringComparison.Ordinal);
    }
}