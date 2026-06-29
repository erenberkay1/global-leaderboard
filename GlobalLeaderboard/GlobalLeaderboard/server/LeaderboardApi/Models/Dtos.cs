namespace LeaderboardApi.Models;

/// <summary>
/// Oyuncudan gelen skor gönderme isteği.
/// Signature = HMAC-SHA256( "playerId:score:timestamp" , HmacSecret ) → küçük harf hex.
/// </summary>
public record SubmitScoreRequest(
    string PlayerId,
    string PlayerName,
    long Score,
    long Timestamp,
    string Signature
);

/// <summary>Skor gönderme sonucu.</summary>
public record SubmitScoreResponse(bool Accepted, int Rank, string Message);

/// <summary>Liste/sıralama görünümü.</summary>
public record ScoreView(int Rank, string PlayerId, string PlayerName, long Score, DateTime CreatedAtUtc);
