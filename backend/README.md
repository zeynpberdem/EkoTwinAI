# EkoTwin – Backend

KOBİ'ler için **İkiz Dönüşüm (Dijital + Yeşil) Karar Destek Sistemi**'nin API katmanı.
Firmalar 16 soruluk bir değerlendirmeyi yanıtlar; sistem 4 boyutta skor ve olgunluk seviyesi hesaplar.

| Boyut | Sorular |
|---|---|
| Dijital Altyapı | 1–4 |
| Enerji Verimliliği | 5–8 |
| Karbon Yönetimi | 9–12 |
| Tedarik Zinciri | 13–16 |

## Teknolojiler

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- PostgreSQL 18 (birincil anahtarlar için yerleşik `uuidv7()` fonksiyonu kullanılır)
- Swagger (API test arayüzü)
- xUnit (birim testleri)

## Klasör yapısı

```
backend/
├── EkoTwin.sln                 Çözüm dosyası (API + testler)
├── EkoTwin/                    API projesi
│   ├── Controllers/            HTTP uç noktaları
│   ├── Data/                   AppDbContext ve başlangıç verisi (16 soru)
│   ├── Dtos/                   API'nin alıp verdiği veri şablonları
│   ├── Migrations/             Veritabanı şeması geçmişi (otomatik üretilir)
│   ├── Models/                 Veritabanı tabloları (entity'ler)
│   ├── Services/               İş mantığı ve skor hesaplama
│   ├── Properties/             Çalıştırma ayarları (port vb.)
│   ├── appsettings.json        Genel ayarlar (şifre İÇERMEZ)
│   └── Program.cs              Uygulamanın başlangıç noktası
└── EkoTwin.Tests/              Birim testleri
```

## Kurulum

### 1. Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 18](https://www.postgresql.org/download/windows/) (kurulumda belirlediğiniz `postgres` şifresini not alın)
- EF Core komut satırı aracı:
  ```
  dotnet tool install --global dotnet-ef --version 8.0.11
  ```

> **Türkçe Windows uyarısı:** PostgreSQL kurulumunun "Locale" ekranında `[Default locale]` seçilirse
> kurulum *"database cluster initialisation failed"* hatasıyla biter (bölge adındaki "ü" harfi nedeniyle).
> Bu ekranda **`C`** veya **`English, United States`** seçin.

### 2. Veritabanı bağlantısı (user-secrets)

Şifre güvenlik nedeniyle `appsettings.json` içinde **tutulmaz**. Her bilgisayarda bir kez,
`backend/EkoTwin` klasöründe şu komutu çalıştırın (`SIFRENIZ` yerine kendi şifrenizi yazın):

```
cd backend/EkoTwin
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ekotwin_db;Username=postgres;Password=SIFRENIZ"
```

User-secrets değeri yalnızca bu bilgisayarda saklanır (`%APPDATA%\Microsoft\UserSecrets\`) ve Git'e gönderilmez.

### 3. Veritabanını oluşturma

```
dotnet ef database update
```

Bu komut `ekotwin_db` veritabanını, tüm tabloları ve 16 soruyu oluşturur.

### 4. Çalıştırma

```
dotnet run --launch-profile http
```

- API: http://localhost:5268/api
- Swagger (test arayüzü): http://localhost:5268/swagger

## API uç noktaları

| Metot | Adres | Açıklama |
|---|---|---|
| GET | `/api/questions` | 16 aktif soruyu sırasıyla getirir |
| GET | `/api/companies` | Firmaları listeler |
| GET | `/api/companies/{id}` | Tek bir firmayı getirir |
| POST | `/api/companies` | Firma ekler |
| GET | `/api/companies/{id}/assessments` | Firmanın geçmiş değerlendirmeleri (yeniden eskiye) |
| POST | `/api/assessments` | 16 yanıtı alır, skorları hesaplar, kaydeder |
| GET | `/api/assessments/{id}` | Değerlendirme raporu |

Hazır örnek istekler için: [`EkoTwin/EkoTwin.http`](EkoTwin/EkoTwin.http)

### Örnek: değerlendirme gönderme

```json
POST /api/assessments
{
  "companyId": "01a1b507-2908-7752-b63c-399033dc9369",
  "answers": [
    { "questionId": "00000000-0000-7000-8000-000000000001", "score": 4 },
    "... 16 sorunun tamamı ..."
  ]
}
```

Kurallar:
- Her aktif soru **tam olarak bir kez** yanıtlanmalıdır.
- Puan **1–5** arasıdır (1 = Hiç / Yok, 5 = Tamamen / İleri düzeyde).
- Kurallara uymayan istekler `400 Bad Request` ve Türkçe hata mesajlarıyla döner.

## Skor hesaplama

| Hesap | Formül |
|---|---|
| Boyut skoru (0–100) | (o boyuttaki 4 yanıtın ortalaması − 1) / 4 × 100 |
| Genel skor (0–100) | 4 boyut skorunun eşit ağırlıklı ortalaması |

| Skor | Olgunluk seviyesi |
|---|---|
| 0 – 19,99 | Başlangıç |
| 20 – 39,99 | Farkındalık |
| 40 – 59,99 | Gelişen |
| 60 – 79,99 | Olgun |
| 80 – 100 | Öncü |

Kurallar tek bir dosyada toplanmıştır: [`EkoTwin/Services/ScoreCalculator.cs`](EkoTwin/Services/ScoreCalculator.cs)

## CBAM (SKDM) risk uyarısı

"AB'ye ihracatta Sınırda Karbon Düzenlemesi'ne takılır mıyım?" sorusuna cevap verir.
Her değerlendirme raporunda `cbamRisk` alanı olarak döner.

Firma kaydında iki alan kullanılır:

| Alan | Değerler |
|---|---|
| `exportsToEu` | `true` / `false` |
| `cbamProductGroup` | `None` (kapsam dışı), `IronAndSteel`, `Aluminium`, `Cement`, `Fertilisers`, `Electricity`, `Hydrogen` |

| Durum | Risk |
|---|---|
| Ürün grubu kapsam dışı (mobilya, tekstil, gıda vb.) | Kapsam dışı |
| Kapsamda, AB'ye ihracat yok | Düşük |
| Kapsamda + AB'ye ihracat, Karbon Yönetimi skoru < 40 | Yüksek |
| Kapsamda + AB'ye ihracat, skor 40 – 69,99 | Orta |
| Kapsamda + AB'ye ihracat, skor ≥ 70 | Düşük |

- Kapsamdaki ürün grupları ve tarihler Tüzük (AB) 2023/956 ve onu değiştiren Tüzük (AB) 2025/2083'e dayanır
  (kesin dönem 1 Ocak 2026'da başladı; ilk sertifika satışı Şubat 2027, ilk yıllık beyan 30 Eylül 2027).
  **Mevzuat değişebilir; güncel metin kontrol edilmelidir.**
- 40 / 70 eşikleri mevzuattan değil, EkoTwin'in tasarım kararından gelir.

Kurallar: [`EkoTwin/Services/CbamRiskEvaluator.cs`](EkoTwin/Services/CbamRiskEvaluator.cs)

## Testler

`backend` klasöründe:

```
dotnet test
```

- `ScoreCalculatorTests` – formül, yuvarlama, olgunluk seviyesi sınır değerleri
- `AssessmentServiceTests` – kayıt, skor hesaplama, eksik/tekrarlı/geçersiz yanıt kontrolleri
- `CbamRiskEvaluatorTests` – CBAM kapsamı, risk seviyesi sınır değerleri, öneriler

Testler gerçek veritabanına dokunmaz; her test bellekte (InMemory) geçici bir veritabanı kullanır.

## Sık kullanılan komutlar

`backend/EkoTwin` klasöründe:

| Amaç | Komut |
|---|---|
| Model değiştikten sonra yeni migration | `dotnet ef migrations add <Isim>` |
| Migration'ları veritabanına uygula | `dotnet ef database update` |
| Son (uygulanmamış) migration'ı geri al | `dotnet ef migrations remove` |
| Migration listesi | `dotnet ef migrations list` |

## Soruları değiştirme

Soru metinleri [`EkoTwin/Data/QuestionSeed.cs`](EkoTwin/Data/QuestionSeed.cs) dosyasındadır.
Metni değiştirdikten sonra:

```
dotnet ef migrations add SoruGuncelleme
dotnet ef database update
```
