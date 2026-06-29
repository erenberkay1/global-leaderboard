using System.Security.Cryptography;
using System.Text;

namespace LeaderboardApi.Security;

/// <summary>
/// HMAC-SHA256 imza üretimi/doğrulaması.
/// ÖNEMLİ: Mesaj formatı Unity client'taki ile BİREBİR aynı olmalı:
///   message = playerId + ":" + score + ":" + timestamp
/// </summary>
public static class HmacValidator
{
    public static string Compute(string secret, string playerId, long score, long timestamp)
    {
        var message = $"{playerId}:{score}:{timestamp}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(string secret, string playerId, long score, long timestamp, string? signature)
    {
        var expected = Compute(secret, playerId, score, timestamp);
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(signature ?? string.Empty);
        // Zamanlama saldırılarına karşı sabit süreli karşılaştırma.
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
