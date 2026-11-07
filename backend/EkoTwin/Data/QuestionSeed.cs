using EkoTwin.Models;

namespace EkoTwin.Data;

/// <summary>
/// Anketin başlangıç soruları (4 boyut × 4 soru = 16 soru).
/// Migration ile veritabanına eklenir. Seed verisinde ID'lerin sabit olması gerektiği için
/// burada elle verilmiştir; sonradan eklenen sorularda ID'yi veritabanı (uuidv7) üretir.
/// Soru metnini değiştirdikten sonra yeni bir migration oluşturmak yeterlidir.
/// </summary>
public static class QuestionSeed
{
    private static Guid Id(int n) => Guid.Parse($"00000000-0000-7000-8000-{n:D12}");

    public static readonly Question[] Questions =
    {
        // ---------- Kategori 1: Dijital Altyapı ----------
        new() { Id = Id(1), DisplayOrder = 1, Dimension = Dimension.DigitalInfrastructure,
            Text = "İş süreçlerinizde (stok, satış, muhasebe) entegre bir ERP veya CRM yazılımı kullanıyor musunuz?" },
        new() { Id = Id(2), DisplayOrder = 2, Dimension = Dimension.DigitalInfrastructure,
            Text = "Üretim veya operasyon verileriniz dijital ortamda anlık olarak toplanıyor mu?" },
        new() { Id = Id(3), DisplayOrder = 3, Dimension = Dimension.DigitalInfrastructure,
            Text = "Siber güvenlik önlemleri (düzenli yedekleme, firewall vb.) aktif olarak uygulanıyor mu?" },
        new() { Id = Id(4), DisplayOrder = 4, Dimension = Dimension.DigitalInfrastructure,
            Text = "Çalışanlarınızın dijital yetkinliklerini artırmak için düzenli eğitimler veriliyor mu?" },

        // ---------- Kategori 2: Enerji Verimliliği ----------
        new() { Id = Id(5), DisplayOrder = 5, Dimension = Dimension.EnergyEfficiency,
            Text = "Tesisinizde enerji tüketimini anlık izleyen sayaç veya otomasyon sistemleri var mı?" },
        new() { Id = Id(6), DisplayOrder = 6, Dimension = Dimension.EnergyEfficiency,
            Text = "Aydınlatma ve ısıtma/soğutma sistemleriniz enerji verimli (LED, akıllı termostat vb.) teknolojiye sahip mi?" },
        new() { Id = Id(7), DisplayOrder = 7, Dimension = Dimension.EnergyEfficiency,
            Text = "Üretim süreçlerinizde atık ısı geri kazanımı veya enerji tasarrufu sağlayan önlemler alınıyor mu?" },
        new() { Id = Id(8), DisplayOrder = 8, Dimension = Dimension.EnergyEfficiency,
            Text = "Enerji verimliliğine yönelik periyodik etütler veya denetimler yapılıyor mu?" },

        // ---------- Kategori 3: Karbon Yönetimi ----------
        new() { Id = Id(9), DisplayOrder = 9, Dimension = Dimension.CarbonManagement,
            Text = "Kapsam 1 (doğrudan) ve Kapsam 2 (dolaylı) emisyonlarınızı hesaplıyor ve takip ediyor musunuz?" },
        new() { Id = Id(10), DisplayOrder = 10, Dimension = Dimension.CarbonManagement,
            Text = "Ürünlerinizin karbon ayak izini (LCA - Yaşam Döngüsü Analizi) ölçümlediniz mi?" },
        new() { Id = Id(11), DisplayOrder = 11, Dimension = Dimension.CarbonManagement,
            Text = "Karbon emisyonlarını azaltmak için somut bir hedefiniz ve azaltım yol haritanız var mı?" },
        new() { Id = Id(12), DisplayOrder = 12, Dimension = Dimension.CarbonManagement,
            Text = "AB Yeşil Mutabakatı ve SKDM (CBAM) kriterlerine uyum düzeyiniz nedir?" },

        // ---------- Kategori 4: Tedarik Zinciri ----------
        new() { Id = Id(13), DisplayOrder = 13, Dimension = Dimension.SupplyChain,
            Text = "Tedarikçilerinizi seçerken sürdürülebilirlik ve çevre kriterlerini dikkate alıyor musunuz?" },
        new() { Id = Id(14), DisplayOrder = 14, Dimension = Dimension.SupplyChain,
            Text = "Lojistik ve taşıma operasyonlarınızda karbon salınımını optimize edecek planlamalar yapılıyor mu?" },
        new() { Id = Id(15), DisplayOrder = 15, Dimension = Dimension.SupplyChain,
            Text = "Hammadde ve malzeme tedarikinde yerel tedarikçilere öncelik veriliyor mu?" },
        new() { Id = Id(16), DisplayOrder = 16, Dimension = Dimension.SupplyChain,
            Text = "Tedarik zincirinizde geri dönüştürülmüş veya çevre dostu malzeme kullanılıyor mu?" },
    };
}
