using EkoTwin.Models;

namespace EkoTwin.Data;

/// <summary>
/// Başlangıç öneri kütüphanesi: 16 soru bazlı aksiyon + 4 boyut bazlı stratejik uyarı = 20 öneri.
///
/// Yazım ilkeleri:
/// - Kaynağı gösterilemeyen yüzde/rakam kullanılmaz; etkiler nitel ifade edilir.
///   Rakam eklenecekse <see cref="Recommendation.Source"/> alanına kaynak yazılmalıdır.
/// - CBAM'e özgü ifadeler yalnızca <see cref="Recommendation.CbamNote"/> alanındadır;
///   bu not sadece AB'ye CBAM kapsamında ürün ihraç eden firmalara gösterilir.
///
/// Metni değiştirdikten sonra yeni migration oluşturmak yeterlidir
/// (dotnet ef migrations add OneriGuncelleme). Eski raporlar değişmez (snapshot).
/// </summary>
public static class RecommendationSeed
{
    private static Guid Id(int n) => Guid.Parse($"00000000-0000-7000-8001-{n:D12}");

    // QuestionSeed ile aynı kimlikler (soru numarası → soru Id)
    private static Guid Q(int questionNumber) => Guid.Parse($"00000000-0000-7000-8000-{questionNumber:D12}");

    private static Recommendation ForQuestion(int n, string title, string rationale,
        Level priority, Level cost, Level impact, ImpactType type, int minMonths, int maxMonths,
        string? cbamNote = null) => new()
    {
        Id = Id(n),
        TriggerType = RecommendationTrigger.Question,
        QuestionId = Q(n),
        Title = title,
        Rationale = rationale,
        Priority = priority,
        CostLevel = cost,
        ImpactLevel = impact,
        ImpactType = type,
        DurationMinMonths = minMonths,
        DurationMaxMonths = maxMonths,
        CbamRelevant = cbamNote is not null,
        CbamNote = cbamNote
    };

    private static Recommendation ForDimension(int n, Dimension dimension, string title, string rationale,
        Level priority, Level cost, Level impact, ImpactType type, int minMonths, int maxMonths,
        string? cbamNote = null) => new()
    {
        Id = Id(n),
        TriggerType = RecommendationTrigger.Dimension,
        Dimension = dimension,
        Title = title,
        Rationale = rationale,
        Priority = priority,
        CostLevel = cost,
        ImpactLevel = impact,
        ImpactType = type,
        DurationMinMonths = minMonths,
        DurationMaxMonths = maxMonths,
        CbamRelevant = cbamNote is not null,
        CbamNote = cbamNote
    };

    public static readonly Recommendation[] Recommendations =
    {
        // =================== Dijital Altyapı (Soru 1-4) ===================
        ForQuestion(1,
            "Bulut tabanlı ön muhasebe ve stok yazılımına geçiş",
            "Satış, stok ve faturaların kâğıt veya dağınık Excel dosyaları yerine tek bir sistemde tutulması " +
            "veri giriş hatalarını ve stok kayıplarını azaltır, güncel duruma her an erişim sağlar. " +
            "İleride entegre ERP'ye geçişin ilk adımıdır.",
            Level.High, Level.Low, Level.High, ImpactType.QuickWin, 1, 2),

        ForQuestion(2,
            "Kritik makinelerde dijital üretim takibi",
            "Makine duruşları, çevrim süreleri ve firelerin sensör veya basit veri toplama terminalleriyle anlık " +
            "izlenmesi üretimdeki darboğazları görünür kılar. Tüm tesis yerine en kritik 1-2 makineyle başlanabilir.",
            Level.Medium, Level.Medium, Level.High, ImpactType.LongTermEfficiency, 2, 4),

        ForQuestion(3,
            "Temel siber güvenlik ve yedekleme paketi",
            "Düzenli ve en az bir kopyası farklı ortamda tutulan yedekleme, güncel güvenlik duvarı ve çok faktörlü " +
            "kimlik doğrulama; fidye yazılımı gibi saldırılarda veri ve üretim kaybını önler. " +
            "Kişisel veri işleyen firmalar için KVKK yükümlülükleri açısından da gereklidir.",
            Level.High, Level.Low, Level.High, ImpactType.QuickWin, 1, 1),

        ForQuestion(4,
            "Çalışanlar için dijital yetkinlik eğitim programı",
            "Yazılım ve otomasyon yatırımlarının verim getirmesi, çalışanların bu araçları benimsemesine bağlıdır. " +
            "Kısa, uygulamalı eğitimlerle başlanabilir; kamu ve meslek odası destekli eğitim programları araştırılmalıdır.",
            Level.Medium, Level.Low, Level.Medium, ImpactType.LongTermEfficiency, 1, 3),

        // =================== Enerji Verimliliği (Soru 5-8) ===================
        ForQuestion(5,
            "IoT tabanlı anlık enerji izleme ve sayaç entegrasyonu",
            "Boşta çalışan makineleri, kaçak akımları ve pik saat tüketimini görünür kılar; enerji faturalarında " +
            "ölçülebilir düşüş sağlar. Diğer tüm enerji verimliliği adımları için gereken verinin temelidir.",
            Level.High, Level.Low, Level.High, ImpactType.QuickWin, 1, 2,
            cbamNote: "CBAM beyanında ürün başına elektrik tüketimi gerekir; hat veya makine bazında ölçüm bu veriyi sağlar."),

        ForQuestion(6,
            "Aydınlatma ve iklimlendirmede verimli ekipmana geçiş",
            "LED aydınlatma, hareket sensörleri ve akıllı termostatlar düşük riskli ve hızlı uygulanabilen yatırımlardır; " +
            "enerji tüketimini üretimi etkilemeden azaltır.",
            Level.Medium, Level.Low, Level.Medium, ImpactType.QuickWin, 1, 2),

        ForQuestion(7,
            "Basınçlı hava, motor ve atık ısı verimlilik iyileştirmeleri",
            "Basınçlı hava kaçaklarının giderilmesi ile yüksek verimli motor ve hız kontrol cihazı (VFD) kullanımı " +
            "düşük maliyetli başlangıçlardır. Döküm, fırın ve kurutma süreçlerinde atık ısının ön ısıtmada " +
            "kullanılması ileri aşama bir adımdır.",
            Level.Medium, Level.Medium, Level.High, ImpactType.LongTermEfficiency, 3, 6),

        ForQuestion(8,
            "Enerji verimliliği etüdü yaptırılması",
            "Yetkili bir enerji verimliliği danışmanlık firmasına yaptırılan etüt, tasarruf fırsatlarını yatırım " +
            "geri dönüş süreleriyle birlikte önceliklendirir. Verimlilik projelerine yönelik destek başvurularında " +
            "da temel belge olarak kullanılır.",
            Level.Medium, Level.Medium, Level.Medium, ImpactType.LongTermEfficiency, 1, 3),

        // =================== Karbon Yönetimi (Soru 9-12) ===================
        ForQuestion(9,
            "Kurumsal karbon ayak izi hesabı (Kapsam 1 ve 2)",
            "Yakıt, doğalgaz ve elektrik faturalarından yıllık sera gazı emisyonunuzu hesaplamak karbon yönetiminin " +
            "ilk adımıdır. Resmi emisyon katsayıları ve hesaplama araçlarıyla düşük maliyetle yapılabilir.",
            Level.High, Level.Low, Level.High, ImpactType.StrategicCompliance, 1, 3,
            cbamNote: "AB'li müşterileriniz CBAM beyanı için ürünlerinizin gömülü emisyon verisini isteyecektir. " +
                      "Veri sunulamazsa yüksek varsayılan değerler uygulanır ve rekabet gücünüz düşer."),

        ForQuestion(10,
            "Ürün bazlı karbon ayak izi (LCA) çalışması",
            "En çok satan ürününüzün yaşam döngüsü boyunca oluşturduğu emisyonu ölçmek; yeşil ürün beyanı, ihale ve " +
            "müşteri taleplerine cevap verebilmenizi sağlar. Uzmanlık gerektirir; kurumsal hesap yapıldıktan sonra " +
            "tek bir üründen başlanması önerilir.",
            Level.Medium, Level.Medium, Level.Medium, ImpactType.StrategicCompliance, 3, 6,
            cbamNote: "CBAM kapsamındaki ürünlerde ürün başına gömülü emisyon hesabı bu çalışmanın temelini oluşturur."),

        ForQuestion(11,
            "Karbon azaltım hedefi ve yol haritası belirlenmesi",
            "Ölçülen emisyonlara dayanan, yıllara yayılmış somut hedefler (enerji verimliliği, yenilenebilir elektrik, " +
            "yakıt değişimi gibi) yatırımları önceliklendirir ve müşterilere taahhüt olarak sunulabilir.",
            Level.Medium, Level.Low, Level.Medium, ImpactType.StrategicCompliance, 1, 3,
            cbamNote: "Ürünlerinizin gömülü emisyonu azaldıkça AB'li müşterinizin CBAM maliyeti, dolayısıyla " +
                      "fiyat rekabetiniz doğrudan iyileşir."),

        ForQuestion(12,
            "AB Yeşil Mutabakatı ve SKDM (CBAM) uyum değerlendirmesi",
            "Ürünlerinizin AB düzenlemelerinden nasıl etkilendiğini, müşterilerinizin hangi veriyi hangi formatta " +
            "isteyeceğini ve Türkiye'deki iklim mevzuatındaki gelişmeleri düzenli takip etmek; sürpriz maliyet ve " +
            "müşteri kaybı riskini azaltır.",
            Level.High, Level.Low, Level.High, ImpactType.StrategicCompliance, 1, 2,
            cbamNote: "CBAM kesin dönemi 1 Ocak 2026'da başladı; 2026 ithalatları için ilk yıllık beyan " +
                      "30 Eylül 2027'dedir. Mevzuat değişebileceği için güncel metin takip edilmelidir."),

        // =================== Tedarik Zinciri (Soru 13-16) ===================
        ForQuestion(13,
            "Yeşil tedarikçi değerlendirme kriterlerinin oluşturulması",
            "Tedarikçi seçiminde fiyat ve kalitenin yanına çevre ve sürdürülebilirlik kriterlerinin eklenmesi, tedarik " +
            "zincirinden kaynaklanan dolaylı emisyonları (Kapsam 3) düşürür ve yeşil kriter arayan müşteri ve " +
            "ihalelere erişimi kolaylaştırır.",
            Level.Medium, Level.Low, Level.Medium, ImpactType.LongTermEfficiency, 2, 4),

        ForQuestion(14,
            "Lojistik planlama ve rota optimizasyonu",
            "Sevkiyatların birleştirilmesi, boş dönüşlerin azaltılması ve rota planlama araçlarının kullanılması " +
            "yakıt maliyetini ve taşımacılık kaynaklı emisyonları birlikte düşürür.",
            Level.Medium, Level.Low, Level.Medium, ImpactType.QuickWin, 1, 3),

        ForQuestion(15,
            "Yerel tedarikçi ağının geliştirilmesi",
            "Aynı il veya OSB'deki tedarikçilere öncelik vermek taşıma mesafesini ve teslim sürelerini kısaltır, " +
            "tedarik kesintisi riskini azaltır. Karar verilirken fiyat ve kaliteyle birlikte değerlendirilmelidir.",
            Level.Low, Level.Low, Level.Low, ImpactType.LongTermEfficiency, 3, 6),

        ForQuestion(16,
            "Geri dönüştürülmüş ve çevre dostu malzeme kullanımının artırılması",
            "İkincil hammadde ve geri dönüştürülmüş ambalaj kullanımı hammadde maliyetini ve ürünün karbon ayak izini " +
            "düşürebilir. Kalite gereksinimleri korunarak kademeli olarak artırılmalıdır.",
            Level.Medium, Level.Medium, Level.Medium, ImpactType.LongTermEfficiency, 3, 6,
            cbamNote: "Demir-çelik ve alüminyumda hurda (ikincil hammadde) oranı ürünün gömülü emisyonunu doğrudan etkiler."),

        // =================== Boyut bazlı stratejik uyarılar (skor < 50) ===================
        ForDimension(17, Dimension.DigitalInfrastructure,
            "Dijital dönüşüm yol haritası ve entegre ERP/CRM planı",
            "Dijital altyapınız temel seviyede. Süreçleri aşamalı olarak tek bir entegre sistemde birleştiren bir plan " +
            "(önce muhasebe ve stok, ardından üretim ve müşteri yönetimi) hazırlanmalı; kamu dijitalleşme destekleri " +
            "araştırılmalıdır.",
            Level.High, Level.Medium, Level.High, ImpactType.LongTermEfficiency, 3, 6),

        ForDimension(18, Dimension.EnergyEfficiency,
            "Enerji yönetim sistemi kurulması (ISO 50001 yaklaşımı)",
            "Enerji verimliliğiniz düşük seviyede. Bir enerji sorumlusu atanması, tüketimin düzenli izlenmesi ve yıllık " +
            "iyileştirme hedeflerinden oluşan bir yönetim sistemi, tek seferlik önlemlerin kalıcı tasarrufa " +
            "dönüşmesini sağlar.",
            Level.High, Level.Medium, Level.High, ImpactType.LongTermEfficiency, 3, 6),

        ForDimension(19, Dimension.CarbonManagement,
            "Karbon yönetimi için acil yol haritası",
            "Karbon yönetiminde temel adımlar eksik. Önce kurumsal emisyon hesabı, ardından azaltım hedefleri ve ürün " +
            "bazlı hesap şeklinde aşamalı bir plan hazırlanmalı; iklim düzenlemeleri ve müşteri talepleri bu alanı " +
            "giderek zorunlu hale getirmektedir.",
            Level.High, Level.Medium, Level.High, ImpactType.StrategicCompliance, 1, 3,
            cbamNote: "AB'ye CBAM kapsamında ürün ihraç ettiğiniz için bu yol haritası öncelikli olarak " +
                      "ürün başına gömülü emisyon verisini üretmeye odaklanmalıdır."),

        ForDimension(20, Dimension.SupplyChain,
            "Sürdürülebilir tedarik zinciri politikası",
            "Tedarik zinciri boyutunuz zayıf. Tedarikçi seçimi, lojistik planlama ve malzeme tercihlerini kapsayan " +
            "yazılı bir sürdürülebilir tedarik politikası, müşterilerin artan tedarik zinciri denetimlerine " +
            "hazırlık sağlar.",
            Level.Medium, Level.Low, Level.Medium, ImpactType.LongTermEfficiency, 3, 6),
    };
}
