using EkoTwin.Models;

namespace EkoTwin.Data;

/// <summary>
/// Anketin başlangıç soruları (4 boyut × 4 soru = 16 soru) ve her sorunun 5 cevap seçeneği.
///
/// Her soru için üç metin tutulur:
///   Text       → resmi/teknik metin (danışman, akademik rapor)
///   SimpleText → KOBİ sahibine gösterilen sade metin (arayüzdeki kart)
///   HelpText   → "Bu ne demek?" açıklaması (kartta ⓘ simgesine tıklanınca açılır)
///
/// Cevap seçenekleri davranışı sorar ("ne yapıyorsunuz?") ve 1'den 5'e merdiven gibi ilerler;
/// veritabanında yine 1-5 puan saklanır, skor hesabı değişmez.
///
/// Metni değiştirdikten sonra yeni bir migration oluşturmak yeterlidir. Eski değerlendirmeler
/// puanla saklandığı için etkilenmez.
/// </summary>
public static class QuestionSeed
{
    private static Guid Id(int n) => Guid.Parse($"00000000-0000-7000-8000-{n:D12}");

    /// <summary>Seçenek kimliği: soru 9, puan 3 → ...-000000000093 (soru 16, puan 5 → ...-000000000165).</summary>
    private static Guid OptionId(int question, int score) => Guid.Parse($"00000000-0000-7000-8003-{question * 10 + score:D12}");

    private sealed record Definition(
        int Number, Dimension Dimension, string Text, string SimpleText, string? HelpText, string[] Options);

    private static readonly Definition[] Definitions =
    {
        // ---------- Kategori 1: Dijital Altyapı ("Dijital düzen") ----------
        new(1, Dimension.DigitalInfrastructure,
            "İş süreçlerinizde (stok, satış, muhasebe) entegre bir ERP veya CRM yazılımı kullanıyor musunuz?",
            "Muhasebe, stok ve satışı bir bilgisayar programında mı takip ediyorsunuz?",
            "Defter ve dağınık dosyalar yerine, işlerinizi tek yerde tutan bir program (ön muhasebe programı veya ERP).",
            new[]
            {
                "Hayır, defter veya kâğıtla tutuyoruz",
                "Excel gibi ayrı dosyalarla tutuyoruz",
                "Bazı işler için program kullanıyoruz",
                "Çoğu işimiz tek programda",
                "Her şey tek programda ve birbirine bağlı"
            }),
        new(2, Dimension.DigitalInfrastructure,
            "Üretim veya operasyon verileriniz dijital ortamda anlık olarak toplanıyor mu?",
            "Ne kadar ürettiğinizi ve makinelerin ne zaman durduğunu kayıt altına alıyor musunuz?",
            "Üretim bilgilerinin kâğıtta değil, bilgisayarda ve zamanında tutulması.",
            new[]
            {
                "Hayır, kayıt tutmuyoruz",
                "Kâğıda yazıyoruz",
                "Gün sonunda bilgisayara giriyoruz",
                "Bazı makinelerden bilgi kendiliğinden geliyor",
                "Tüm üretimi anlık olarak ekrandan izliyoruz"
            }),
        new(3, Dimension.DigitalInfrastructure,
            "Siber güvenlik önlemleri (düzenli yedekleme, firewall vb.) aktif olarak uygulanıyor mu?",
            "Bilgisayarlarınızı ve kayıtlarınızı virüse ve veri kaybına karşı koruyor musunuz?",
            "Düzenli yedek almak, virüs programı ve güçlü şifre kullanmak (siber güvenlik).",
            new[]
            {
                "Hiçbir önlem yok",
                "Sadece virüs programı var",
                "Ara sıra yedek alıyoruz",
                "Düzenli yedek alıyoruz, şifre kurallarımız var",
                "Düzenli yedek ve koruma var, bundan sorumlu bir kişi var"
            }),
        new(4, Dimension.DigitalInfrastructure,
            "Çalışanlarınızın dijital yetkinliklerini artırmak için düzenli eğitimler veriliyor mu?",
            "Çalışanlarınıza bilgisayar ve program kullanımı konusunda eğitim veriyor musunuz?",
            null,
            new[]
            {
                "Hiç vermedik",
                "Gerektiğinde birbirlerinden öğreniyorlar",
                "Yeni program gelince eğitim veriyoruz",
                "Yılda bir-iki kez eğitim yapıyoruz",
                "Düzenli bir eğitim planımız var"
            }),

        // ---------- Kategori 2: Enerji Verimliliği ("Enerji tasarrufu") ----------
        new(5, Dimension.EnergyEfficiency,
            "Tesisinizde enerji tüketimini anlık izleyen sayaç veya otomasyon sistemleri var mı?",
            "Ne kadar elektrik ve yakıt harcadığınızı faturadan başka bir yolla takip ediyor musunuz?",
            "Bölüm veya makine bazında ayrı sayaç, harcamayı anlık gösteren ekran.",
            new[]
            {
                "Hayır, sadece faturayı ödüyoruz",
                "Faturaları aydan aya karşılaştırıyoruz",
                "Bazı bölümlerde ayrı sayaç var",
                "Önemli makinelerde sayaç var, düzenli bakıyoruz",
                "Anlık izliyoruz ve tasarruf hedefi koyuyoruz"
            }),
        new(6, Dimension.EnergyEfficiency,
            "Aydınlatma ve ısıtma/soğutma sistemleriniz enerji verimli (LED, akıllı termostat vb.) teknolojiye sahip mi?",
            "Aydınlatma, ısıtma ve soğutmada tasarruflu ürünler kullanıyor musunuz?",
            "LED lamba, hareket sensörü, ayarlı termostat, yalıtım gibi.",
            new[]
            {
                "Hayır, hepsi eski sistem",
                "Birkaç lambayı değiştirdik",
                "Yaklaşık yarısı tasarruflu",
                "Büyük kısmı tasarruflu",
                "Tamamı tasarruflu ve kendiliğinden ayarlanıyor"
            }),
        new(7, Dimension.EnergyEfficiency,
            "Üretim süreçlerinizde atık ısı geri kazanımı veya enerji tasarrufu sağlayan önlemler alınıyor mu?",
            "Üretimde enerji israfını azaltmak için önlem alıyor musunuz?",
            "Basınçlı hava kaçaklarını gidermek, boşta çalışan makineleri kapatmak, fırın ve ocak ısısını tekrar kullanmak.",
            new[]
            {
                "Hayır, bu konuya hiç bakmadık",
                "Boşta çalışan makineleri kapatıyoruz",
                "Kaçakları ve arızaları hemen onarıyoruz",
                "Tasarruflu motor veya kompresör aldık",
                "Bunlara ek olarak atık ısıyı da geri kazanıyoruz"
            }),
        new(8, Dimension.EnergyEfficiency,
            "Enerji verimliliğine yönelik periyodik etütler veya denetimler yapılıyor mu?",
            "Enerji tasarrufu için tesisinizi bir uzmana inceletiniz mi?",
            "Enerji verimliliği etüdü: uzman nerede israf olduğunu ölçer ve rapor verir.",
            new[]
            {
                "Hayır, hiç düşünmedik",
                "Düşünüyoruz ama yaptırmadık",
                "Bir kez yaptırdık",
                "Yaptırdık ve önerileri uyguladık",
                "Birkaç yılda bir düzenli yaptırıyoruz"
            }),

        // ---------- Kategori 3: Karbon Yönetimi ("Karbon takibi") ----------
        new(9, Dimension.CarbonManagement,
            "Kapsam 1 (doğrudan) ve Kapsam 2 (dolaylı) emisyonlarınızı hesaplıyor ve takip ediyor musunuz?",
            "Firmanızın bir yılda ne kadar karbon saldığını hesapladınız mı?",
            "Yaktığınız yakıttan ve kullandığınız elektrikten çıkan karbonun hesabı (karbon ayak izi). EkoTwin'in karbon hesaplayıcısıyla da yapabilirsiniz.",
            new[]
            {
                "Hayır, hiç hesaplamadık",
                "Ne olduğunu biliyoruz ama hesaplamadık",
                "Bir kez kabaca hesapladık",
                "Her yıl hesaplıyoruz",
                "Her yıl hesaplıyor ve uzmana doğrulatıyoruz"
            }),
        new(10, Dimension.CarbonManagement,
            "Ürünlerinizin karbon ayak izini (LCA - Yaşam Döngüsü Analizi) ölçümlediniz mi?",
            "Sattığınız ürünlerin karbonunu ayrı ayrı hesapladınız mı?",
            "Bir ürünün hammaddesinden müşteriye ulaşana kadar çıkan karbon (ürün karbon ayak izi). Avrupalı müşteriler bunu giderek daha çok soruyor.",
            new[]
            {
                "Hayır",
                "Müşterimiz sordu ama cevap veremedik",
                "Bir ürün için kabaca hesapladık",
                "Ana ürünlerimiz için hesapladık",
                "Tüm ürünlerimiz için hesaplı ve belgeli"
            }),
        new(11, Dimension.CarbonManagement,
            "Karbon emisyonlarını azaltmak için somut bir hedefiniz ve azaltım yol haritanız var mı?",
            "Karbonunuzu azaltmak için bir hedefiniz ve planınız var mı?",
            "Örneğin \"2030'a kadar yüzde 20 azaltacağız\" demek ve bunun için atılacak adımları yazmak.",
            new[]
            {
                "Hayır",
                "Düşünüyoruz ama yazılı değil",
                "Hedefimiz var, planımız yok",
                "Hedefimiz de planımız da var",
                "Planı uyguluyoruz, her yıl kontrol ediyoruz"
            }),
        new(12, Dimension.CarbonManagement,
            "AB Yeşil Mutabakatı ve SKDM (CBAM) kriterlerine uyum düzeyiniz nedir?",
            "Avrupa'ya mal satarken istenen karbon kurallarına ne kadar hazırsınız?",
            "AB'nin Sınırda Karbon Düzenlemesi (SKDM / CBAM). Demir-çelik, alüminyum, çimento gibi ürünlerde Avrupalı alıcı ürünün karbon bilgisini istiyor. Avrupa'ya satışınız yoksa bile müşterileriniz ileride sorabilir.",
            new[]
            {
                "Bu kuralları hiç duymadık",
                "Duyduk ama ne yapacağımızı bilmiyoruz",
                "Bilgi topluyoruz",
                "Hazırlanıyoruz, müşterimize bilgi verebiliyoruz",
                "Tamamen hazırız"
            }),

        // ---------- Kategori 4: Tedarik Zinciri ("Tedarikçiler ve malzeme") ----------
        new(13, Dimension.SupplyChain,
            "Tedarikçilerinizi seçerken sürdürülebilirlik ve çevre kriterlerini dikkate alıyor musunuz?",
            "Malzeme aldığınız firmaları seçerken çevreye verdikleri önemi dikkate alıyor musunuz?",
            "Fiyat ve kalitenin yanında çevre belgesine (örneğin ISO 14001) ve atıklarını nasıl yönettiklerine bakmak.",
            new[]
            {
                "Hayır, sadece fiyata bakıyoruz",
                "Bazen aklımıza geliyor",
                "Önemli tedarikçilerimize soruyoruz",
                "Yazılı kurallarımız var",
                "Yazılı kurallarımız var ve düzenli kontrol ediyoruz"
            }),
        new(14, Dimension.SupplyChain,
            "Lojistik ve taşıma operasyonlarınızda karbon salınımını optimize edecek planlamalar yapılıyor mu?",
            "Mal taşımayı ve sevkiyatı yakıt tasarrufunu düşünerek planlıyor musunuz?",
            "Araçları dolu göndermek, yolu kısaltmak, boş dönüşleri azaltmak.",
            new[]
            {
                "Hayır, plan yapmıyoruz",
                "Bazen sevkiyatları birleştiriyoruz",
                "Sevkiyatları önceden planlıyoruz",
                "Araç doluluğuna ve güzergâha bakıyoruz",
                "Bir programla planlıyor ve yakıtı takip ediyoruz"
            }),
        new(15, Dimension.SupplyChain,
            "Hammadde ve malzeme tedarikinde yerel tedarikçilere öncelik veriliyor mu?",
            "Malzemeleri mümkün olduğunca yakındaki firmalardan mı alıyorsunuz?",
            "Yakından almak hem taşımadan çıkan karbonu hem de bekleme süresini azaltır.",
            new[]
            {
                "Hayır, çoğu uzaktan veya yurt dışından",
                "Az bir kısmı yakından",
                "Yarı yarıya",
                "Çoğunu yakından alıyoruz",
                "Yakından alınabilecek her şeyi yakından alıyoruz"
            }),
        new(16, Dimension.SupplyChain,
            "Tedarik zincirinizde geri dönüştürülmüş veya çevre dostu malzeme kullanılıyor mu?",
            "Geri dönüştürülmüş ya da çevre dostu malzeme kullanıyor musunuz?",
            "Hurda metal, geri dönüştürülmüş plastik veya kâğıt, belgeli ahşap gibi.",
            new[]
            {
                "Hayır",
                "Çok az",
                "Bazı ürünlerimizde",
                "Ürünlerimizin çoğunda",
                "Mümkün olan her yerde"
            })
    };

    public static readonly Question[] Questions = Definitions
        .Select(d => new Question
        {
            Id = Id(d.Number),
            DisplayOrder = d.Number,
            Dimension = d.Dimension,
            Text = d.Text,
            SimpleText = d.SimpleText,
            HelpText = d.HelpText
        })
        .ToArray();

    public static readonly AnswerOption[] Options = Definitions
        .SelectMany(d => d.Options.Select((label, i) => new AnswerOption
        {
            Id = OptionId(d.Number, i + 1),
            QuestionId = Id(d.Number),
            Score = i + 1,
            Label = label
        }))
        .ToArray();
}
