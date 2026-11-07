# 🌿 EkoTwin AI

> **Bridging Industry & Earth:** AI-Driven Twin Transformation & Decision Support Engine for SMEs

**EkoTwin AI**, KOBİ'lerin dijitalleşme ve yeşil dönüşüm (İkiz Dönüşüm) olgunluk seviyelerini ölçen, operasyonel darboğazları tespit eden ve yapay zeka entegrasyonuyla kişiselleştirilmiş stratejik eylem planları sunan **Karar Destek Sistemidir (KDS)**.

---

## 🚀 Proje Öne Çıkanları

* **4 Temel Olgunluk Boyutu:** Dijital Altyapı, Enerji Verimliliği, Karbon Yönetimi ve Tedarik Zinciri süreçlerinde bütüncül olgunluk analizi.
* **Akıllı Darboğaz Tespiti (Bottleneck Analysis):** KOBİ'nin dönüşümünü yavaşlatan en zayıf halkanın matematiksel olarak belirlenmesi.
* **Görsel Analiz (MEXT Standartları):** Chart.js ile dinamik Radar Grafiği (Spider Chart) üzerinden olgunluk skorlaması.
* **Yapay Zeka Danışmanı:** Hesaplanan skor ve darboğaz verilerine göre AB Yeşil Mutabakatı (CBAM) uyumlu aksiyon planı üreten Gemini API entegrasyonu.

---

## 🛠 Teknoloji Yığını (Tech Stack)

| Katman | Teknoloji / Kütüphane |
| :--- | :--- |
| **Backend** | C# .NET 8 (ASP.NET Core MVC) |
| **Database** | PostgreSQL |
| **ORM** | Entity Framework Core (Npgsql) |
| **Frontend** | HTML5, Bootstrap 5, JavaScript |
| **Data Viz** | Chart.js (Radar Chart) |
| **AI Integration** | Gemini API |

---

## 📂 Proje Mimari Yapısı

Pragmatik ve bakımı kolay **Unified (Monolithic)** klasör yapısı kullanılmıştır:

```text
EkoTwin AI/
├── Models/              # PostgreSQL Entity tanımları (Company, Assessment vb.)
├── Data/                # DbContext ve EF Core PostgreSQL konfigürasyonları
├── Services/            # Skorlama motoru ve Gemini API entegrasyon servisleri
├── Controllers/         # MVC Controller yapıları ve istek yönlendirmeleri
├── Views/               # Razor View arayüz tasarımları ve Radar Chart entegrasyonu
└── wwwroot/             # Statik dosyalar (CSS, JS, Görseller)
