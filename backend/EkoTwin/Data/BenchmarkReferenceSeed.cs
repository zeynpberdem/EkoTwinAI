using EkoTwin.Models;

namespace EkoTwin.Data;

/// <summary>
/// Türkiye geneli referans değerleri: TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması.
/// Araştırma 10 ve üzeri çalışanı olan girişimleri kapsar; mikro firmalar (1-9) için referans yoktur.
///
/// DOĞRULAMA DURUMU (8 Kasım 2026): Değerler TÜİK haber bülteni metnini aktaran kaynaklardan
/// (Anadolu Ajansı, Alomaliye) alındı; TÜİK veri portalındaki resmi tablo henüz açılamadı.
/// Bu yüzden hepsi IsVerified = false: kullanıcıya gösterilmez. Resmi tablo ile teyit edildikten
/// sonra IsVerified = true yapılıp yeni migration oluşturulur.
///
/// Eşleşme: ERP ↔ Soru 1'de 4 ("Çoğu işimiz tek programda") ve 5 ("Her şey tek programda ve birbirine bağlı").
/// Güvenlik önlemi ↔ Soru 3'te 2 ve üstü (en az bir önlem). Bulut kullanımı hiçbir cevapla birebir
/// eşleşmediği için yalnızca bilgi olarak gösterilir.
/// </summary>
public static class BenchmarkReferenceSeed
{
    private static Guid Id(int n) => Guid.Parse($"00000000-0000-7000-8004-{n:D12}");

    private const string Ict2025 = "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025";
    private const string Ict2026 = "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026";

    private const string ErpIndicator = "ERP yazılımı kullanan girişim oranı";
    private const string ErpSimple = "işlerini birbirine bağlı tek bir programda yürütüyor";
    private const string CloudIndicator = "Ücretli bulut bilişim hizmeti kullanan girişim oranı";
    private const string CloudSimple = "programlarını internet üzerinden ücretli olarak kullanıyor";
    private const string SecurityIndicator = "Bilgi ve iletişim teknolojileri güvenlik önlemi alan girişim oranı";
    private const string SecuritySimple = "bilgisayarları için en az bir güvenlik önlemi alıyor";

    public static readonly BenchmarkReference[] References =
    {
        new() { Id = Id(1), SizeClass = SizeClass.Small, Indicator = ErpIndicator, SimpleIndicator = ErpSimple,
                Percent = 23.6m, Year = 2025, QuestionNumber = 1, MinAnswerScore = 4, Source = Ict2025, IsVerified = false },
        new() { Id = Id(2), SizeClass = SizeClass.Medium, Indicator = ErpIndicator, SimpleIndicator = ErpSimple,
                Percent = 46.0m, Year = 2025, QuestionNumber = 1, MinAnswerScore = 4, Source = Ict2025, IsVerified = false },
        new() { Id = Id(3), SizeClass = SizeClass.Small, Indicator = CloudIndicator, SimpleIndicator = CloudSimple,
                Percent = 16.7m, Year = 2026, Source = Ict2026, IsVerified = false },
        new() { Id = Id(4), SizeClass = SizeClass.Medium, Indicator = CloudIndicator, SimpleIndicator = CloudSimple,
                Percent = 33.3m, Year = 2026, Source = Ict2026, IsVerified = false },
        new() { Id = Id(5), SizeClass = null, Indicator = SecurityIndicator, SimpleIndicator = SecuritySimple,
                Percent = 83.0m, Year = 2026, QuestionNumber = 3, MinAnswerScore = 2, Source = Ict2026, IsVerified = false },
    };
}
