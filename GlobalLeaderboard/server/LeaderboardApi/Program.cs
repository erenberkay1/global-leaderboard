using LeaderboardApi.Data;
using LeaderboardApi.Models;
using LeaderboardApi.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 0) BULUT PORTU  (Render/Railway gibi servisler PORT ortam değişkeni verir;
//    yoksa lokal varsayılan port kullanılır.)
// ---------------------------------------------------------------------------
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ---------------------------------------------------------------------------
// 1) AYARLAR  (appsettings.json -> "Leaderboard" bölümünden okunur)
// ---------------------------------------------------------------------------
var apiKey = builder.Configuration["Leaderboard:ApiKey"] ?? "CHANGE_ME_API_KEY";
var hmacSecret = builder.Configuration["Leaderboard:HmacSecret"] ?? "CHANGE_ME_HMAC_SECRET";
var maxScore = builder.Configuration.GetValue<long>("Leaderboard:MaxScore", 1_000_000_000);
var timestampTolerance = builder.Configuration.GetValue<long>("Leaderboard:TimestampToleranceSeconds", 300);

// ---------------------------------------------------------------------------
// 2) VERİTABANI  (SQLite - sıfır kurulum, dosya otomatik oluşur)
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=leaderboard.db";
builder.Services.AddDbContext<LeaderboardDbContext>(options => options.UseSqlite(connectionString));

// ---------------------------------------------------------------------------
// 3) RATE LIMIT  (spam/flood koruması: 10 sn'de 20 istek)
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", o =>
    {
        o.Window = TimeSpan.FromSeconds(10);
        o.PermitLimit = 20;
        o.QueueLimit = 0;
    });
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// 4) VERİTABANINI HAZIRLA  (migration'a gerek yok, tablo otomatik kurulur)
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LeaderboardDbContext>();
    db.Database.EnsureCreated();
}

app.UseRateLimiter();

// ---------------------------------------------------------------------------
// 5) API KEY KONTROLÜ  (/ ve /health hariç tüm istekler X-Api-Key ister)
// ---------------------------------------------------------------------------
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path == "/" || path == "/health")
    {
        await next();
        return;
    }

    var providedKey = context.Request.Headers["X-Api-Key"].ToString();
    if (string.IsNullOrEmpty(providedKey) || providedKey != apiKey)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Invalid or missing API key." });
        return;
    }

    await next();
});

// ---------------------------------------------------------------------------
// 6) ENDPOINT'LER
// ---------------------------------------------------------------------------
app.MapGet("/", () => Results.Ok(new { service = "Global Leaderboard API", status = "running" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// --- Skor gönder ---
app.MapPost("/scores", async (SubmitScoreRequest req, LeaderboardDbContext db) =>
{
    // Girdi doğrulama
    if (string.IsNullOrWhiteSpace(req.PlayerId) || req.PlayerId.Length > 64)
        return Results.BadRequest(new SubmitScoreResponse(false, 0, "Invalid playerId."));

    if (string.IsNullOrWhiteSpace(req.PlayerName) || req.PlayerName.Length > 32)
        return Results.BadRequest(new SubmitScoreResponse(false, 0, "Invalid playerName."));

    if (req.Score < 0 || req.Score > maxScore)
        return Results.BadRequest(new SubmitScoreResponse(false, 0, "Score out of allowed range."));

    // Zaman damgası tazeliği (replay koruması)
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    if (Math.Abs(now - req.Timestamp) > timestampTolerance)
        return Results.BadRequest(new SubmitScoreResponse(false, 0, "Request expired. Check device clock."));

    // HMAC imza doğrulaması (kurcalama koruması)
    if (!HmacValidator.Verify(hmacSecret, req.PlayerId, req.Score, req.Timestamp, req.Signature))
        return Results.BadRequest(new SubmitScoreResponse(false, 0, "Invalid signature."));

    // Oyuncunun yalnızca EN İYİ skorunu sakla
    var existing = await db.Scores.FirstOrDefaultAsync(s => s.PlayerId == req.PlayerId);
    if (existing is null)
    {
        db.Scores.Add(new ScoreEntry
        {
            PlayerId = req.PlayerId,
            PlayerName = req.PlayerName,
            Score = req.Score,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
    else
    {
        existing.PlayerName = req.PlayerName;
        if (req.Score > existing.Score)
        {
            existing.Score = req.Score;
            existing.CreatedAtUtc = DateTime.UtcNow;
        }
    }
    await db.SaveChangesAsync();

    // Güncel sıralamayı hesapla
    var bestScore = await db.Scores
        .Where(s => s.PlayerId == req.PlayerId)
        .Select(s => s.Score)
        .FirstAsync();
    var rank = await db.Scores.CountAsync(s => s.Score > bestScore) + 1;

    return Results.Ok(new SubmitScoreResponse(true, rank, "Score accepted."));
}).RequireRateLimiting("fixed");

// --- En iyi skorlar (JsonUtility ile uyumlu olması için { "items": [...] } döner) ---
app.MapGet("/scores/top", async (int? count, LeaderboardDbContext db) =>
{
    var take = Math.Clamp(count ?? 10, 1, 100);
    var top = await db.Scores
        .OrderByDescending(s => s.Score)
        .ThenBy(s => s.CreatedAtUtc)
        .Take(take)
        .ToListAsync();

    var items = top
        .Select((s, i) => new ScoreView(i + 1, s.PlayerId, s.PlayerName, s.Score, s.CreatedAtUtc))
        .ToList();

    return Results.Ok(new { items });
}).RequireRateLimiting("fixed");

// --- Tek bir oyuncunun sıralaması ---
app.MapGet("/scores/rank/{playerId}", async (string playerId, LeaderboardDbContext db) =>
{
    var entry = await db.Scores.FirstOrDefaultAsync(s => s.PlayerId == playerId);
    if (entry is null)
        return Results.NotFound(new { message = "Player not found." });

    var rank = await db.Scores.CountAsync(s => s.Score > entry.Score) + 1;
    return Results.Ok(new ScoreView(rank, entry.PlayerId, entry.PlayerName, entry.Score, entry.CreatedAtUtc));
}).RequireRateLimiting("fixed");

app.Run();
