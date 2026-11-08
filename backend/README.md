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
│   ├── Data/                   AppDbContext ve başlangıç verisi (16 soru, 20 öneri, 7 emisyon faktörü)
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
| DELETE | `/api/companies/{id}` | Firmayı, **tüm değerlendirmelerini, karbon kayıtlarını ve hedefini** kalıcı olarak siler |
| GET | `/api/companies/{id}/assessments` | Firmanın geçmiş değerlendirmeleri (yeniden eskiye) |
| POST | `/api/assessments` | 16 yanıtı alır, skorları hesaplar, kaydeder |
| GET | `/api/assessments/{id}` | Değerlendirme raporu |
| GET | `/api/recommendations` | Öneri kütüphanesi (20 şablon) |
| GET | `/api/emission-factors` | Emisyon faktörleri (kaynak, birim, faktör, resmi kaynak) |
| GET | `/api/companies/{id}/carbon-footprints` | Firmanın yıllık karbon kayıtları (yeniden eskiye) |
| POST | `/api/companies/{id}/carbon-footprints` | Yıllık karbon hesabı (aynı yıl varsa **409**) |
| GET | `/api/carbon-footprints/{id}` | Karbon ayak izi raporu |
| PUT | `/api/carbon-footprints/{id}` | Hesabı yeni değerlerle yeniden yapar |
| DELETE | `/api/carbon-footprints/{id}` | Karbon kaydını siler |
| GET | `/api/companies/{id}/progress` | Gelişim özeti: geçmiş, son karşılaştırma, öneri takibi, karbon trendi, hedef |
| GET | `/api/assessments/{id}/compare/{otherId}` | Aynı firmanın iki değerlendirmesini karşılaştırır |
| GET | `/api/companies/{id}/carbon-target` | Karbon azaltım hedefi ve ilerlemesi |
| PUT | `/api/companies/{id}/carbon-target` | Hedefi oluşturur / günceller (firma başına tek hedef) |
| DELETE | `/api/companies/{id}/carbon-target` | Hedefi siler |
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

| Skor | Olgunluk seviyesi | Sade ad (`simpleMaturityLevel`) | Kademe (`maturityStage`) |
|---|---|---|---|
| 0 – 19,99 | Başlangıç | Yolun başında | 1 |
| 20 – 39,99 | Farkındalık | Farkında | 2 |
| 40 – 59,99 | Gelişen | Gelişiyor | 3 |
| 60 – 79,99 | Olgun | İyi durumda | 4 |
| 80 – 100 | Öncü | Örnek firma | 5 |

Kademe (1-5) arayüzde renk ve ikon seçimi için kullanılır.

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

## Karbon hesaplayıcı (Kapsam 1 + 2)

"Karbon salınımım ne?" sorusuna cevap verir. Firma **yıllık fatura değerlerini** girer, sistem
GHG Protocol'e göre kurumsal karbon ayak izini hesaplar.

| Kaynak | Birim | kg CO2e / birim | Kapsam |
|---|---|---|---|
| Elektrik (şebekeden) | kWh | 0,478 | 2 |
| Doğal gaz | m³ | 1,9372 | 1 |
| Motorin (dizel) | litre | 2,6853 | 1 |
| Benzin | litre | 2,2952 | 1 |
| LPG | kg | 2,9872 | 1 |
| Kok kömürü | ton | 3.036,5055 | 1 |
| Linyit | ton | 1.209,9623 | 1 |

**Faktörlerin kaynağı**
- Elektrik: T.C. Enerji ve Tabii Kaynaklar Bakanlığı, *2022 Yılı Elektrik Üretim ve Tüketim Noktası Emisyon
  Faktörleri*, dağıtım hattına bağlı tüketim noktası (0,478 tCO2e/MWh).
- Yakıtlar: IPCC 2006 Rehberi Cilt 2, Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma);
  CH4 ve N2O, IPCC AR5 küresel ısınma potansiyelleriyle (28 / 265) CO2e'ye çevrilir.
  Doğal gaz için 9.155 kcal/Sm³ referans üst ısıl değer (EPDK/BOTAŞ) × 0,90 = alt ısıl değer.
- Faktörler elle yazılmaz, bu parametrelerden hesaplanır: [`EkoTwin/Data/EmissionFactorSeed.cs`](EkoTwin/Data/EmissionFactorSeed.cs)

**Hesap:** satır emisyonu (t) = miktar × faktör ÷ 1000 (3 hane). Kapsam 1 = yakıtlar, Kapsam 2 = elektrik.
Toplam satırların toplamıdır; ayrıca çalışan başına emisyon, en büyük kaynak ve payı, kaynağa göre
sade dilde yönlendirme, **önceki yılla karşılaştırma** ve CBAM kapsamındaki ihracatçılar için not döner.

**Kurallar**
- Firma başına yıl başına tek kayıt (veritabanında tekil index); gelecek yıl girilemez.
- Her hesap kullandığı faktörü ve kaynağını satırında saklar: faktör güncellense de eski hesap değişmez.
  PUT ile yeniden hesaplanırsa güncel faktörler kullanılır.

**Sınırlar (raporda da gösterilir):** Kapsam 3, süreç emisyonları (kireçtaşı, elektrot vb.) ve soğutucu gaz
kaçakları dahil değildir; sonuç ön değerlendirmedir, resmi beyan için akredite doğrulama gerekir.

Örnek istek:

```json
POST /api/companies/{id}/carbon-footprints
{
  "year": 2025,
  "entries": [
    { "source": "Electricity", "quantity": 380000 },
    { "source": "NaturalGas",  "quantity": 30000 },
    { "source": "Coke",        "quantity": 160 },
    { "source": "Diesel",      "quantity": 5500 }
  ]
}
```

Sonuç: Kapsam 1 = 558,726 t, Kapsam 2 = 181,64 t, toplam **740,366 t CO2e**; en büyük kaynak kok kömürü (%65,6).

## Gelişim takibi

"Geçen sefere göre ne değişti? Önerileri yaptık mı? Karbonumuz azalıyor mu?" sorularına cevap verir.
`GET /api/companies/{id}/progress` tek istekle gelişim sayfasının tüm verisini döner.

**Değerlendirme karşılaştırması** (son iki değerlendirme veya `compare` ile herhangi ikisi)
- Genel skor ve olgunluk seviyesi değişimi, 4 boyutun değişimi, en çok gelişen / geriye giden boyut
- Yalnızca puanı değişen sorular (soru numarasıyla eşleşir)

**Öneri takibi (otomatik, firmadan ek bilgi istenmez)**

| Önceki öneri | Durum |
|---|---|
| Soru bazlı, yeni yanıt ≥ 3 | İyileşti |
| Boyut bazlı, yeni boyut skoru ≥ 50 | İyileşti |
| Koşul devam ediyor | Hâlâ açık |
| Önceki raporda yoktu, bu sefer çıktı | Yeni (bir alan gerilemiş) |

Eşikler öneri motorunun tetikleme eşikleriyle aynıdır. A3'teki öneri snapshot'ı sayesinde
"geçen sefer size şu önerilmişti" bilgisi şablonlar değişse bile korunur.

**Karbon trendi ve hedef**
- Yıllara göre seri, ilk yıla (baz) göre toplam ve çalışan başına değişim
- Hedef: baz yıl + hedef yıl + azaltma yüzdesi (baz yıl için karbon kaydı olmalı)

| Hesap | Formül |
|---|---|
| Hedef emisyon | Baz × (1 − hedef % / 100) |
| Gerçekleşen azalma | (Baz − son yıl) / Baz × 100 |
| Beklenen azalma | Hedef % × geçen yıl / toplam süre (doğrusal yol) |
| Durum | Ulaşıldı · Yolunda (gerçekleşen ≥ beklenen) · Geride |
| Tahmini yıl | Bugüne kadarki yıllık ortalama hızla hedefin tamamlanacağı yıl |

**Özet maddeleri:** Sade dilde 3-7 madde (skor değişimi, seviye, en çok gelişen/geriye giden boyut,
öneri takibi, 180 günü geçen değerlendirme için yenileme hatırlatması, karbon değişimi, hedef durumu).

Kurallar: [`ProgressAnalyzer.cs`](EkoTwin/Services/ProgressAnalyzer.cs), [`CarbonTargetEvaluator.cs`](EkoTwin/Services/CarbonTargetEvaluator.cs)

## Sade dil (her yaştan KOBİ sahibi için)

Kullanıcıya gösterilen metinler teknik değil, günlük dille yazılır. Teknik terimler kaybolmaz,
yalnızca "Bu ne demek?" açıklamasına (arayüzde ⓘ simgesi) taşınır.

**Sorular** (`GET /api/questions`): her soruda

| Alan | Kullanım |
|---|---|
| `simpleText` | Kartta gösterilen sade soru |
| `helpText` | ⓘ ile açılan açıklama (teknik terim burada, parantez içinde) |
| `options` | 5 anlamlı cevap (`label`); seçilen seçeneğin `score` değeri (1-5) gönderilir |
| `text` | Resmi / teknik metin (danışman, akademik rapor) |

Seçenekler görüş değil davranış sorar ("ne yapıyorsunuz?") ve 1'den 5'e merdiven gibi ilerler.
Skor hesabı değişmez. Raporda her yanıtın seçilen cevap metni (`answerLabel`), boyutların sade adı
(Dijital düzen, Enerji tasarrufu, Karbon takibi, Tedarikçiler ve malzeme) ve sade seviye adı döner.

**Öneriler** (raporda ve `GET /api/recommendations`): her öneride teknik alanların yanında

| Alan | Örnek |
|---|---|
| `simpleTitle` | "Firmanızın yıllık karbonunu hesaplayın" |
| `simpleBenefit` | "Müşteri veya banka sorduğunda cevabınız hazır olur." |
| `simpleCbamNote` | Avrupa notu; yalnızca CBAM kapsamındaki ihracatçılara |
| `simpleReasons` | "Cevabınız: 'Hayır, hiç hesaplamadık'", "En çok zorlandığınız konu: Karbon takibi", "Az masraflı" |
| `simpleCostText` | Az masraflı / Orta masraflı / Yüksek masraflı |
| `answerLabel` | Öneriyi tetikleyen seçilen cevap |

Sade alanlar da rapor anındaki hâliyle saklanır (snapshot). Bu özellikten önce üretilmiş raporlara
migration sırasında şablondan sade başlık ve kazanım kopyalanır.

**Raporlar** (karbon, Avrupa/CBAM, gelişim): sade cümle kalıpları tek dosyada,
[`PlainLanguage.cs`](EkoTwin/Services/PlainLanguage.cs). Teknik alanlar ve kesin rakamlar korunur.

| Rapor | Sade alanlar | Örnek |
|---|---|---|
| Karbon | `simple` (başlık, yakıt, elektrik, çalışan başına, en büyük kaynak, öneri, karşılaştırma, Avrupa notu, sınır), kalemlerde `simpleScopeName`, `simpleTonnes` | "Kendi yaktığınız yakıtlardan: 559 ton" |
| Avrupa (CBAM) | `simpleRiskLevel`, `simpleSummary`, `simpleActions` | "Yüksek risk", "Avrupa'ya sattığınız demir-çelik ürünleri için karbon bilgisi istenecek…" |
| Hedef | `simpleStatusName`, `simpleSummary` | "Hedefinize doğru iyi gidiyorsunuz… Bu hızla hedefe 2027 yılında ulaşırsınız." |
| Gelişim | `simpleHighlights`, öneri takibinde `simpleTitle`/`simpleDetail`, soru değişiminde seçilen cevaplar | "Önce: “Hayır, hiç hesaplamadık” → Şimdi: “Her yıl hesaplıyoruz”" |

Yazım kuralları: tonlar yuvarlanır (10 tonun üstü tam sayı), yüzdeler "yüzde 11" biçiminde yazılır,
yıllardan sonra ek yerine "2027 yılında" denir. Öneri takibinde "yaptınız" değil "ilerleme var" denir;
durum firmanın yeni cevaplarından çıkarılır, işin yapıldığı ayrıca doğrulanmaz.

**Ölçüm ve koruma** ([`QuestionSeedTests`](EkoTwin.Tests/QuestionSeedTests.cs))
- Jargon testi: sade soru ve seçeneklerde ERP, CBAM, emisyon, entegre vb. geçemez.
- Uzunluk: sade soru ≤ 15 kelime, seçenek ≤ 10 kelime.
- Ateşman okunabilirlik formülü (1997) metin bütünlerine uygulanır
  ([`Readability.cs`](EkoTwin/Services/Readability.cs)):

| Metin bütünü | Ateşman | Seviye |
|---|---|---|
| Resmi soru metinleri | 54,9 | Orta güçlükte |
| Sade soru metinleri | 58,4 | Orta güçlükte |
| Cevap seçenekleri | 77,1 | Kolay (test eşiği ≥ 70) |
| "Bu ne demek?" açıklamaları | 63,9 | Orta güçlükte (test eşiği ≥ 50) |
| Öneri açıklamaları (teknik `rationale`) | 37,1 | Zor |
| Öneri kazanımları (sade `simpleBenefit`) | 64,0 | Orta güçlükte (test eşiği ≥ 50) |
| Avrupa ve karbon metinleri (teknik) | 50,7 | Orta güçlükte |
| Avrupa ve karbon metinleri (sade) | 65,2 | Orta güçlükte |
| Gelişim maddeleri (teknik → sade) | 77,5 → 78,9 | Kolay (sade, teknikten kolay olmalı; eşik ≥ 50) |

Not: Ateşman yalnızca kelime/cümle uzunluğuna bakar. "ERP", "LCA" gibi kısaltmaları kısa (kolay) sayar,
uzun Türkçe kelimeleri cezalandırır. Bu yüzden resmi ve sade sorular arasındaki fark küçük çıkar; asıl
sadeleşme jargon testiyle güvence altındadır. Formül tek cümlede de güvenilir değildir (paragraflar
için tasarlanmıştır). Kesin değerlendirme için gerçek kullanıcılarla kullanılabilirlik testi planlanmıştır.

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
- `CarbonCalculatorTests` – karbon formülü, kapsam ayrımı, faktör değerlerinin IPCC parametrelerinden elle hesapla doğrulanması
- `CarbonFootprintServiceTests` – kayıt, yıl çakışması, doğrulama, önceki yılla karşılaştırma, faktör snapshot'ı, CBAM notu
- `CarbonApiTests` – karbon uç noktaları uçtan uca (201/400/404/409)
- `ProgressServiceTests` – karşılaştırma, otomatik öneri takibi, gelişim özeti, karbon trendi, hedef kuralları
- `CarbonTargetEvaluatorTests` – hedef durumu (ulaşıldı / yolunda / geride), tahmini yıl, sınır durumlar
- `ProgressApiTests` – gelişim, karşılaştırma ve hedef uç noktaları uçtan uca
- `QuestionSeedTests` – soru/seçenek yapısı, jargon yasağı, uzunluk sınırları, okunabilirlik
- `ReadabilityTests` – Ateşman formülünün elle hesapla doğrulanması
- `ReportSimpleLanguageTests` – karbon, Avrupa, hedef ve gelişim raporlarının sade cümleleri; jargon taraması
- `RecommendationSimpleLanguageTests` – öneri sade metinleri (jargon, uzunluk, okunabilirlik) ve sade gerekçeler
- `SimpleLanguageApiTests` – sade metin, açıklama, seçenekler ve rapordaki cevap metni uçtan uca
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

Soru metinleri (resmi, sade, açıklama) ve cevap seçenekleri [`EkoTwin/Data/QuestionSeed.cs`](EkoTwin/Data/QuestionSeed.cs) dosyasındadır.
Metni değiştirdikten sonra:

```
dotnet ef migrations add SoruGuncelleme
dotnet ef database update
```
