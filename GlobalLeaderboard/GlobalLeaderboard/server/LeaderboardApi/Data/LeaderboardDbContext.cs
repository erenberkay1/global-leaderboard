using LeaderboardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardApi.Data;

public class LeaderboardDbContext : DbContext
{
    public LeaderboardDbContext(DbContextOptions<LeaderboardDbContext> options) : base(options) { }

    public DbSet<ScoreEntry> Scores => Set<ScoreEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScoreEntry>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.PlayerId).IsRequired().HasMaxLength(64);
            e.Property(s => s.PlayerName).IsRequired().HasMaxLength(32);
            e.HasIndex(s => s.Score);
            e.HasIndex(s => s.PlayerId);
        });
    }
}
