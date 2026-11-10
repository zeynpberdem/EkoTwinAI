using EkoTwin.Models;

namespace EkoTwin.Data;

/// <summary>
/// Türkiye geneli referans değerleri: TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması.
/// Araştırma 10 ve üzeri çalışanı olan girişimleri kapsar; mikro firmalar (1-9) için referans yoktur.
///
/// DOĞRULAMA: Tüm değerler 8 Kasım 2026'da TÜİK'in resmi haber bülteni metinleriyle birebir karşılaştırıldı:
///   2025 bülteni: https://veriportali.tuik.gov.tr/tr/press/54012
///   2026 bülteni: https://veriportali.tuik.gov.tr/tr/press/58021 (bir sonraki bülten: Eylül 2027)
///
/// EŞLEŞME KURALI: Tanım bizim soruyla örtüşüyorsa firmanın cevabıyla eşleştirilir ("Siz de bunlardan
/// birisiniz"); örtüşmüyorsa yalnızca bilgi olarak gösterilir (yanlış hüküm vermemek için).
///   ERP ↔ Soru 1'de 4-5 ("Çoğu işimiz tek programda", "Her şey tek programda ve birbirine bağlı")
///   Güvenlik önlemi ↔ Soru 3'te 2+ (en az bir önlem)
///   Bulut kullanımı → yalnızca bilgi (hiçbir soruyla eşleşmez)
///   Enerji için bilişim sistemi → şimdilik yalnızca bilgi (Soru 5 ile kısmen örtüşür; kullanıcı kararı)
/// </summary>
public static class BenchmarkReferenceSeed
{
    private static Guid Id(int n) => Guid.Parse($"00000000-0000-7000-8004-{n:D12}");

    private const string Ict2025 =
        "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025 (https://veriportali.tuik.gov.tr/tr/press/54012)";
    private const string Ict2026 =
        "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026 (https://veriportali.tuik.gov.tr/tr/press/58021)";

    private const string ErpIndicator = "ERP yazılımı kullanan girişim oranı";
    private const string ErpSimple = "işlerini birbirine bağlı tek bir programda yürütüyor";
    private const string CloudIndicator = "Ücretli bulut bilişim hizmeti kullanan girişim oranı";
    private const string CloudSimple = "programlarını internet üzerinden ücretli olarak kullanıyor";
    private const string SecurityIndicator = "Bilgi ve iletişim teknolojileri güvenlik önlemi alan girişim oranı";
    private const string SecuritySimple = "bilgisayarları için en az bir güvenlik önlemi alıyor";
    private const string EnergyIndicator = "Enerji tüketimini azaltmak için bilgi ve iletişim teknolojileri kullanan girişim oranı";
    private const string EnergySimple = "enerjiyi azaltmak için yazılım veya akıllı sistem kullanıyor";

    public static readonly BenchmarkReference[] References =
    {
        // 2025: "Bu oranlar, 10-49 çalışanı olan girişimlerde sırasıyla %23,6, %9,9 ve %4,9,
        //        50-249 çalışanı olan girişimlerde ise %46,0, %18,4 ve %10,7 oldu." (ERP, CRM, BI)
        new() { Id = Id(1), SizeClass = SizeClass.Small, Indicator = ErpIndicator, SimpleIndicator = ErpSimple,
                Percent = 23.6m, Year = 2025, QuestionNumber = 1, MinAnswerScore = 4, Source = Ict2025, IsVerified = true },
        new() { Id = Id(2), SizeClass = SizeClass.Medium, Indicator = ErpIndicator, SimpleIndicator = ErpSimple,
                Percent = 46.0m, Year = 2025, QuestionNumber = 1, MinAnswerScore = 4, Source = Ict2025, IsVerified = true },

        // 2026: "…bunu %33,3 ile 50-249 çalışanı olan girişimlerin, %16,7 ile 10-49 çalışanı olan girişimlerin takip ettiği görüldü."
        new() { Id = Id(3), SizeClass = SizeClass.Small, Indicator = CloudIndicator, SimpleIndicator = CloudSimple,
                Percent = 16.7m, Year = 2026, Source = Ict2026, IsVerified = true },
        new() { Id = Id(4), SizeClass = SizeClass.Medium, Indicator = CloudIndicator, SimpleIndicator = CloudSimple,
                Percent = 33.3m, Year = 2026, Source = Ict2026, IsVerified = true },

        // 2026: "10-49 çalışanı olan girişimlerin %81,2'sinin, 50-249 çalışanı olan girişimlerin %90,5'inin … bir güvenlik önlemi aldığı görüldü."
        new() { Id = Id(5), SizeClass = SizeClass.Small, Indicator = SecurityIndicator, SimpleIndicator = SecuritySimple,
                Percent = 81.2m, Year = 2026, QuestionNumber = 3, MinAnswerScore = 2, Source = Ict2026, IsVerified = true },
        new() { Id = Id(6), SizeClass = SizeClass.Medium, Indicator = SecurityIndicator, SimpleIndicator = SecuritySimple,
                Percent = 90.5m, Year = 2026, QuestionNumber = 3, MinAnswerScore = 2, Source = Ict2026, IsVerified = true },

        // 2025: "10-49 çalışanı olan girişimlerin %15,0'ının, 50-249 çalışanı olan girişimlerin %24,3'ünün … enerji verimliliği
        //        için bilgi ve iletişim teknolojileri sistemleri veya çözümleri kullandığı görüldü."
        new() { Id = Id(7), SizeClass = SizeClass.Small, Indicator = EnergyIndicator, SimpleIndicator = EnergySimple,
                Percent = 15.0m, Year = 2025, Source = Ict2025, IsVerified = true },
        new() { Id = Id(8), SizeClass = SizeClass.Medium, Indicator = EnergyIndicator, SimpleIndicator = EnergySimple,
                Percent = 24.3m, Year = 2025, Source = Ict2025, IsVerified = true },
    };
}
