using System.Security.Cryptography;
using System.Text;

namespace GlobalLeaderboard
{
    /// <summary>
    /// Produces an HMAC-SHA256 signature. The message format MUST match the server
    /// exactly: message = playerId + ":" + score + ":" + timestamp
    /// Output: lowercase hex.
    /// </summary>
    public static class LeaderboardHmac
    {
        public static string Compute(string secret, string playerId, long score, long timestamp)
        {
            string message = playerId + ":" + score + ":" + timestamp;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
