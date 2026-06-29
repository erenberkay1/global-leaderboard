# Global Leaderboard — Unity + ASP.NET Core

Unity için global skor tablosu (leaderboard). İki parçadan oluşur:

- **`server/`** — ASP.NET Core (.NET 8) Web API. Alıcı kendi sunucusuna kurar.
- **`unity/`** — Unity'ye eklenecek C# istemci scriptleri.

> **Önemli:** Bu bir **şablondur**. Sunucuyu satın alan geliştirici kendi
> Azure/AWS/başka sunucusuna kurar. Sorumluluk dağılımı için `DISCLAIMER.md`
> dosyasını mutlaka okuyun.

---

## 0) Bilgisayarına neyi kurman lazım

| Ne | Nereden | Ne için |
|----|---------|---------|
| **.NET 8 SDK** | https://dotnet.microsoft.com/download/dotnet/8.0 | API'yi çalıştırmak/test etmek |
| **Unity** (2020.1+ önerilir, LTS) | https://unity.com/download | Oyun tarafı |
| **Visual Studio 2022** *veya* **VS Code** | https://visualstudio.microsoft.com — VS Code için C# Dev Kit eklentisi | Kodu açıp düzenlemek |
| **Git** (opsiyonel) | https://git-scm.com | GitHub'a yüklemek için |

---

## 1) Sunucuyu LOKALDE çalıştır (5 dakika)

Terminal / komut istemcisini aç ve şunu yaz:

```bash
cd server/LeaderboardApi
dotnet restore
dotnet run
```

İlk çalıştırmada `leaderboard.db` dosyası otomatik oluşur (SQLite, ekstra
kurulum yok). Terminalde şuna benzer bir satır göreceksin:

```
Now listening on: http://localhost:5xxx
```

Tarayıcıdan `http://localhost:5xxx/health` aç → `{"status":"healthy"}` görmelisin.

### Ayarları değiştir
`server/LeaderboardApi/appsettings.json` içinde:

```json
"Leaderboard": {
  "ApiKey": "CHANGE_ME_API_KEY",        // kendi rastgele anahtarınla değiştir
  "HmacSecret": "CHANGE_ME_HMAC_SECRET", // kendi uzun rastgele sırınla değiştir
  "MaxScore": 1000000000,
  "TimestampToleranceSeconds": 300
}
```

> `ApiKey` ve `HmacSecret` değerlerini **mutlaka** değiştir ve Unity tarafındaki
> aynı iki alanla **birebir aynı** yap.

---

## 2) Unity'ye bağla

1. `unity/` klasöründeki **4 dosyayı** Unity projende `Assets/` altına bir
   `Leaderboard` klasörü açıp içine kopyala:
   - `LeaderboardModels.cs`
   - `LeaderboardHmac.cs`
   - `LeaderboardClient.cs`
   - `LeaderboardDemo.cs`
2. Sahnede boş bir **GameObject** oluştur.
3. Üstüne **LeaderboardClient** scriptini ekle. Inspector'da:
   - `Base Url` → `http://localhost:5xxx` (lokal test) veya canlı sunucu adresin
   - `Api Key` → appsettings.json'daki ile aynı
   - `Hmac Secret` → appsettings.json'daki ile aynı
4. Aynı GameObject'e **LeaderboardDemo** scriptini de ekle.
5. **Play**'e bas, **Console**'u izle. Skor gönderilir ve liste yazdırılır.

---

## 3) API uç noktaları (endpoints)

Tümü `X-Api-Key` header ister (`/` ve `/health` hariç).

| Method | Yol | Açıklama |
|--------|-----|----------|
| GET | `/health` | Sağlık kontrolü |
| POST | `/scores` | Skor gönder (HMAC imzalı) |
| GET | `/scores/top?count=10` | En iyi N skor |
| GET | `/scores/rank/{playerId}` | Bir oyuncunun sırası |

GET uç noktalarını tarayıcı/Postman ile test edebilirsin (sadece `X-Api-Key`
header'ı ekle). Skor göndermek imza gerektirdiği için en kolay test Unity'den.

---

## 4) Canlı sunucuya yükleme (alıcı için)

### Seçenek A — Azure App Service
1. Azure'da bir **App Service (.NET 8)** oluştur.
2. `dotnet publish -c Release` çıktısını yükle (VS'de sağ tık → Publish ya da
   GitHub Actions).
3. App Service → Configuration'da `Leaderboard__ApiKey` ve
   `Leaderboard__HmacSecret` ortam değişkenlerini ayarla (`:` yerine `__`).

### Seçenek B — Railway / Render (en hızlı)
1. Repoyu GitHub'a yükle.
2. Railway/Render'da "New from GitHub repo" → otomatik build.
3. Ortam değişkenlerini panelden gir.

### Seçenek C — Docker
Bir `Dockerfile` ekleyip herhangi bir buluta atabilirsin (isteğe bağlı).

> SQLite tek dosyalı; kalıcı disk olmayan platformlarda veriler resetlenebilir.
> Kalıcılık için PostgreSQL'e geçiş önerilir (sadece bağlantı dizesi + paket
> değişir).

---

## 5) Güvenlik notları (dürüst liste)

- **API Key**: kaba erişim kontrolü. Build içinde gömülü olduğundan tek başına
  yeterli değildir.
- **HMAC imza**: skor gövdesinin kurcalanmasını engeller (Postman'le rastgele
  skor atılamaz). Ama sır client içinde gömülü; kararlı bir saldırgan çıkarabilir.
- **Rate limit**: flood/spam korumasıdır.
- **Daha ileri**: sunucu-otoriter skor doğrulama, oyuncu hesabı + JWT, ya da
  skorları oyun-içi olay loglarından sunucuda yeniden hesaplama. Bunlar bu
  şablonun kapsamı dışında ama yol haritasına yazılabilir.

Pazarlamada **"güvenli özellikler içerir"** de, **"kırılamaz/hilesiz"** deme.

---

## 6) Lisans

MIT (`LICENSE`). Sorumluluk dağılımı için `DISCLAIMER.md`.
