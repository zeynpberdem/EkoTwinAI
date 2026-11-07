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
│   ├── Infrastructure/         Hata yönetimi, sağlık kontrolü çıktısı
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
| GET | `/api/companies` | Firmaları listeler. Filtre: `?sector=FoodAndBeverage&search=keban` |
| GET | `/api/companies/{id}` | Tek bir firmayı getirir |
| POST | `/api/companies` | Firma ekler |
| PUT | `/api/companies/{id}` | Firmayı günceller (tüm alanlar gönderilir) |
| DELETE | `/api/companies/{id}` | Firmayı ve **tüm değerlendirmelerini** kalıcı olarak siler |
| GET | `/api/companies/{id}/assessments` | Firmanın geçmiş değerlendirmeleri (yeniden eskiye) |
| POST | `/api/assessments` | 16 yanıtı alır, skorları hesaplar, kaydeder |
| GET | `/api/assessments/{id}` | Değerlendirme raporu |
| GET | `/api/recommendations` | Öneri kütüphanesi (20 şablon) |
| GET | `/api/lookups/sectors` | Sektör listesi (değer + Türkçe ad) |
| GET | `/api/lookups/cbam-product-groups` | CBAM ürün grupları |
| GET | `/api/lookups/dimensions` | 4 boyut |
| GET | `/health` | Uygulama ve veritabanı durumu (200 Healthy / 503 Unhealthy) |

### Sektörler

Sektör serbest metin değil, standart listeden seçilir (sektörel karşılaştırma için gerekli).
Ayrıntı gerekiyorsa isteğe bağlı `subSector` alanına yazılır (örn. "Alüminyum döküm").

`MetalWorkingAndCasting` Metal İşleme ve Döküm · `MachineryAndEquipment` Makine ve Ekipman ·
`AutomotiveSupply` Otomotiv Yan Sanayi · `FurnitureAndWood` Mobilya ve Ağaç Ürünleri ·
`FoodAndBeverage` Gıda ve İçecek · `TextileAndApparel` Tekstil ve Hazır Giyim ·
`MiningAndMarble` Madencilik ve Mermer · `ConstructionMaterials` Yapı Malzemeleri ·
`ChemicalsAndPlastics` Kimya ve Plastik · `ElectricalAndElectronics` Elektrik ve Elektronik ·
`Energy` Enerji · `Other` Diğer

### Hata yanıtları

Tüm hatalar standart **ProblemDetails** (RFC 9457) biçiminde, Türkçe başlıkla döner:

| Kod | Ne zaman |
|---|---|
| 400 | Doğrulama hatası (alan adına göre `errors` listesi ile) |
| 404 | Kayıt veya adres bulunamadı |
| 500 | Beklenmeyen hata (ayrıntı loglara yazılır) |
| 503 | Veritabanına ulaşılamıyor |

Geliştirme ortamında yanıta teknik ayrıntı (`exception`) eklenir; canlı ortamda eklenmez.

### CORS

Frontend'in API'yi çağırabileceği adresler `Cors:AllowedOrigins` ayarındadır.
Geliştirmede (`appsettings.Development.json`) `localhost:5173` (Vite), `:3000` (React) ve `:4200` (Angular) izinlidir.
Canlı ortamda liste boştur; frontend'in gerçek adresi eklenmelidir.

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

## Öneri motoru

Değerlendirme sonucundan firmaya özel, önceliklendirilmiş öneriler üretir. Her raporda `recommendations` alanı olarak döner:

| Alan | İçerik |
|---|---|
| `bottleneckDimension` | En zayıf boyut (darboğaz) |
| `topActions` | **En acil 3 adım** |
| `actions` | Tetiklenen tüm somut aksiyonlar (sıralı) |
| `strategicAlerts` | Boyut bazlı stratejik uyarılar (sıralı) |

### Tetikleme (hibrit)

| Tür | Koşul | Sonuç |
|---|---|---|
| Soru bazlı | Soruya 1 veya 2 puan verildi | Somut aksiyon (16 sorunun her birinin bir önerisi var) |
| Boyut bazlı | Boyut skoru 50'nin altında | Stratejik uyarı (4 boyutun her birinin bir uyarısı var) |

### Aciliyet puanı ve sıralama

| Kriter | Puan |
|---|---|
| Etki düzeyi (Düşük / Orta / Yüksek) | +2 / +4 / +6 |
| Maliyet (Yüksek / Orta / Düşük) | +1 / +2 / +3 |
| Öncelik (Düşük / Orta / Yüksek) | +0 / +1 / +2 |
| Ciddiyet: soru 1 puan / 2 puan — boyut skoru < 25 / diğer | +2 / +1 — +2 / +1 |
| Darboğaz boyutundaysa | +2 |
| CBAM riski Yüksek ve öneri CBAM ilişkiliyse | +3 (öncelik Yüksek'e çıkar) |

- **En acil 3 adım** en yüksek puanlı 3 *aksiyondan* seçilir (stratejik uyarılar ayrı listelenir).
- Eşitlikte: daha düşük yanıt/skor → daha düşük maliyet → soru sırası.
- Her önerinin `reasons` alanı neden o sırada olduğunu açıklar (örn. "Soru 9 yanıtınız: 1/5 · En zayıf boyutunuz: Karbon Yönetimi · CBAM riskiniz yüksek").
- `cbamNote` yalnızca AB'ye CBAM kapsamında ürün ihraç eden firmalara gösterilir.

### Öneri kütüphanesi

- 20 öneri veritabanında (`Recommendations` tablosu) tutulur; başlangıç içeriği
  [`EkoTwin/Data/RecommendationSeed.cs`](EkoTwin/Data/RecommendationSeed.cs) dosyasındadır.
- Her öneride: başlık, gerekçe, öncelik, maliyet, etki düzeyi, etki türü (Hızlı Kazanım / Stratejik Uyum /
  Uzun Vadeli Verimlilik), süre, isteğe bağlı CBAM notu ve kaynak.
- Kaynağı gösterilemeyen yüzde/rakam kullanılmaz (testle denetlenir); rakam eklenecekse `Source` alanı doldurulmalıdır.
- **Raporlar sabitlenir:** üretilen öneriler `AssessmentRecommendations` tablosuna o anki haliyle kaydedilir.
  Şablon metni sonradan değişse de eski raporlar değişmez.

Kurallar: [`EkoTwin/Services/RecommendationEngine.cs`](EkoTwin/Services/RecommendationEngine.cs)

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
- `CompanyServiceTests` – firma ekleme, güncelleme, silme, sektör/ad filtresi
- `GlobalExceptionHandlerTests` – hangi hatanın hangi HTTP koduna dönüştüğü
- `RecommendationEngineTests` – tetikleme, aciliyet puanı, sıralama, en acil 3 adım, darboğaz ve CBAM etkisi
- `RecommendationSeedTests` – öneri kütüphanesinin bütünlüğü (her soruya bir öneri, CBAM ifadeleri, kaynaksız yüzde yok)
- `ApiIntegrationTests` – uygulamayı HTTP üzerinden uçtan uca çalıştırır: sağlık kontrolü, CORS,
  hata yanıtları, firma uç noktaları, açılır listeler

Testler gerçek veritabanına dokunmaz; her test bellekte (InMemory) geçici bir veritabanı kullanır.
InMemory, PostgreSQL'e özgü kuralları (CHECK kısıtı, CASCADE silme, `uuidv7()`) uygulamaz;
bunlar gerçek veritabanında ayrıca denenmiştir.

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
