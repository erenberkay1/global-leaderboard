namespace LeaderboardApi.Models;

/// <summary>
/// Veritabanında saklanan tek bir skor kaydı (her oyuncunun en iyi skoru).
/// </summary>
public class ScoreEntry
{
    public int Id { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public long Score { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
