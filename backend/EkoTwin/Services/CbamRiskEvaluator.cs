using EkoTwin.Dtos;
using EkoTwin.Models;

namespace EkoTwin.Services;

/// <summary>
/// SKDM / CBAM risk değerlendirmesi: "AB'ye ihracatta CBAM'e takılır mıyım?" sorusuna cevap verir.
///
/// Mevzuat özeti (Kasım 2026 itibarıyla; değişebilir, güncel metin için Tüzük (AB) 2023/956
/// ve onu değiştiren Tüzük (AB) 2025/2083 esas alınmalıdır):
/// - Kapsam: demir-çelik, alüminyum, çimento, gübre, elektrik, hidrojen.
/// - Kesin dönem 1 Ocak 2026'da başladı; 2026 ithalatları için sertifika satışı
///   1 Şubat 2027'de açılır, ilk yıllık beyan 30 Eylül 2027'dedir.
/// - Yükümlülük AB'deki ithalatçıdadır. İthalatçı başına yıllık 50 ton muafiyeti
///   (elektrik ve hidrojen hariç) vardır. Türk ihracatçı dolaylı etkilenir: AB'li müşteri
///   ürünün gömülü emisyon verisini ister ve maliyeti fiyata yansıtır.
///
/// Risk seviyesi eşikleri (EkoTwin tasarım kararı, mevzuattan gelmez):
/// kapsamdaki ve AB'ye ihracat yapan firmada Karbon Yönetimi skoru
/// &lt; 40 → Yüksek, &lt; 70 → Orta, ≥ 70 → Düşük.
/// </summary>
public static class CbamRiskEvaluator
{
    public const string OutOfScope = "Kapsam dışı";
    public const string Low = "Düşük";
    public const string Medium = "Orta";
    public const string High = "Yüksek";

    public static CbamRiskResponse Evaluate(bool exportsToEu, CbamProductGroup group, decimal carbonScore)
    {
        // 1) Ürün grubu kapsam dışı
        if (group == CbamProductGroup.None)
        {
            return new CbamRiskResponse(
                IsInScope: false,
                ProductGroup: group,
                ProductGroupName: DisplayName(group),
                RiskLevel: OutOfScope,
                Summary: "Ürün grubunuz şu an CBAM kapsamında değil. Yine de AB'li müşteriler tedarik zinciri " +
                         "raporlaması için karbon verisi isteyebilir ve kapsamın genişletilmesi gündemdedir.",
                Actions: new[]
                {
                    "Enerji ve yakıt tüketim kayıtlarınızı düzenli tutmaya başlayın; ileride karbon hesabının temeli olacak."
                });
        }

        // 2) Kapsamdaki ürün ama AB'ye ihracat yok
        if (!exportsToEu)
        {
            return new CbamRiskResponse(
                IsInScope: true,
                ProductGroup: group,
                ProductGroupName: DisplayName(group),
                RiskLevel: Low,
                Summary: $"{DisplayName(group)} CBAM kapsamında, ancak AB'ye ihracat yapmadığınız için şu an doğrudan " +
                         "etkilenmiyorsunuz. AB pazarına açılmayı planlıyorsanız hazırlığa şimdiden başlamanız önerilir.",
                Actions: new[]
                {
                    "AB'ye ihracat planınız varsa ürün başına emisyon hesaplama altyapısını önceden kurun."
                });
        }

        // 3) Kapsamdaki ürün + AB'ye ihracat → Karbon Yönetimi skoruna göre risk
        var level = carbonScore switch
        {
            < 40m => High,
            < 70m => Medium,
            _ => Low
        };

        var summary = level switch
        {
            High => $"{DisplayName(group)} ürünlerini AB'ye ihraç ediyorsunuz ve Karbon Yönetimi skorunuz düşük " +
                    $"({carbonScore:0.##}). AB'li müşterileriniz CBAM beyanı için ürün bazında gömülü emisyon verisi " +
                    "isteyecek; veri sunamazsanız varsayılan (yüksek) değerler kullanılır ve rekabet gücünüz düşer.",
            Medium => $"{DisplayName(group)} ürünlerini AB'ye ihraç ediyorsunuz. Karbon yönetiminde temel adımlar atılmış " +
                      $"({carbonScore:0.##}), ancak CBAM'in istediği ürün bazlı ve doğrulanabilir veri için eksikler var.",
            _ => $"{DisplayName(group)} ürünlerini AB'ye ihraç ediyorsunuz ve karbon yönetiminiz güçlü " +
                 $"({carbonScore:0.##}). CBAM veri taleplerine hazırlıklı görünüyorsunuz."
        };

        var actions = new List<string>
        {
            "AB'li müşterilerinizle CBAM için hangi emisyon verisini, hangi formatta isteyeceklerini görüşün.",
            "Ürün başına gömülü emisyonu (doğrudan + elektrik kaynaklı) hesaplayın.",
            "Enerji, yakıt ve hammadde tüketimlerini ürün/hat bazında kayıt altına alın."
        };

        if (level != Low)
            actions.Add("Emisyon verilerinizi bağımsız bir doğrulayıcıya doğrulatmayı planlayın.");

        if (group is CbamProductGroup.Electricity or CbamProductGroup.Hydrogen)
            actions.Add("Elektrik ve hidrojende 50 tonluk muafiyet uygulanmaz; her miktar kapsamdadır.");

        return new CbamRiskResponse(
            IsInScope: true,
            ProductGroup: group,
            ProductGroupName: DisplayName(group),
            RiskLevel: level,
            Summary: summary,
            Actions: actions);
    }

    /// <summary>Ürün grubunun Türkçe adı.</summary>
    public static string DisplayName(CbamProductGroup group) => group switch
    {
        CbamProductGroup.None => "Kapsam dışı ürün",
        CbamProductGroup.IronAndSteel => "Demir-çelik",
        CbamProductGroup.Aluminium => "Alüminyum",
        CbamProductGroup.Cement => "Çimento",
        CbamProductGroup.Fertilisers => "Gübre",
        CbamProductGroup.Electricity => "Elektrik",
        CbamProductGroup.Hydrogen => "Hidrojen",
        _ => group.ToString()
    };
}
