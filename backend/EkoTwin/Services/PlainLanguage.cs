using EkoTwin.Models;
using static EkoTwin.Services.TurkishFormat;

namespace EkoTwin.Services;

/// <summary>
/// Raporlardaki sade dil kalıpları (KOBİ sahibine gösterilen cümleler) tek yerde.
///
/// Kurallar:
/// - Teknik terim yok (Kapsam, CO2e, baz yıl, CBAM...): bunlar yalnızca teknik alanlarda kalır.
/// - Rakamlar yuvarlanır: 10 tonun üstü tam sayı ("740 ton"), altı tek ondalık ("4,8 ton").
/// - Yüzdeler "yüzde 11" biçiminde, tam sayı yazılır.
/// - Yıllardan sonra ek getirmek yerine "2027 yılında" / "2024 yılından bu yana" denir
///   (ünlü uyumu hatası riskini ortadan kaldırır).
/// </summary>
public static class PlainLanguage
{
    /// <summary>Örn. 740.366 → "740 ton", 4.83 → "4,8 ton", 0.04 → "0,1 tondan az".</summary>
    public static string Tonnes(decimal tonnes)
    {
        var abs = Math.Abs(tonnes);
        if (abs > 0 && abs < 0.1m) return "0,1 tondan az";
        return abs >= 10m
            ? $"{Number(Math.Round(tonnes, 0, MidpointRounding.AwayFromZero), "#,0")} ton"
            : $"{Number(Math.Round(tonnes, 1, MidpointRounding.AwayFromZero), "0.#")} ton";
    }

    /// <summary>Örn. 10.9 → "yüzde 11" (işaretsiz).</summary>
    public static string Percent(decimal percent) =>
        $"yüzde {Number(Math.Round(Math.Abs(percent), 0, MidpointRounding.AwayFromZero), "0")}";

    /// <summary>
    /// İyelik ekli yüzde: 23.6 → "yüzde 24'ü", 46 → "yüzde 46'sı", 16.7 → "yüzde 17'si".
    /// Ek, sayının okunuşundaki son kelimeye göre seçilir (yirmi dört → dördü, kırk altı → altısı).
    /// </summary>
    public static string PercentPossessive(decimal percent)
    {
        var n = (int)Math.Round(Math.Abs(percent), 0, MidpointRounding.AwayFromZero);
        return $"yüzde {n}{PossessiveSuffix(n)}";
    }

    /// <summary>0-100 arası tam sayı için "-(s)I" iyelik eki (kesme işaretiyle).</summary>
    public static string PossessiveSuffix(int n)
    {
        if (n == 100) return "'ü";        // yüz → yüzü
        if (n % 10 != 0)
            return (n % 10) switch
            {
                1 => "'i",   // bir → biri
                2 => "'si",  // iki → ikisi
                3 => "'ü",   // üç → üçü
                4 => "'ü",   // dört → dördü
                5 => "'i",   // beş → beşi
                6 => "'sı",  // altı → altısı
                7 => "'si",  // yedi → yedisi
                8 => "'i",   // sekiz → sekizi
                _ => "'u"    // dokuz → dokuzu
            };
        return n switch
        {
            0 => "'ı",       // sıfır → sıfırı
            10 => "'u",      // on → onu
            20 => "'si",     // yirmi → yirmisi
            30 => "'u",      // otuz → otuzu
            40 => "'ı",      // kırk → kırkı
            50 => "'si",     // elli → ellisi
            60 => "'ı",      // altmış → altmışı
            70 => "'i",      // yetmiş → yetmişi
            80 => "'i",      // seksen → sekseni
            _ => "'ı"        // doksan → doksanı
        };
    }

    /// <summary>Puanı tam sayıya yuvarlar: 43.75 → "44".</summary>
    public static string Points(decimal score) => Number(Math.Round(score, 0, MidpointRounding.AwayFromZero), "0");

    public static string ScopeName(EmissionScope scope) => scope switch
    {
        EmissionScope.Scope1 => "Kendi yaktığınız yakıtlar",
        EmissionScope.Scope2 => "Kullandığınız elektrik",
        _ => scope.ToString()
    };

    /// <summary>Kaynak adının "-den/-dan" hâli (ek elle yazılır, ünlü uyumu hatası olmasın diye).</summary>
    public static string SourceAblative(EmissionSource source) => source switch
    {
        EmissionSource.Electricity => "elektrikten",
        EmissionSource.NaturalGas => "doğal gazdan",
        EmissionSource.Diesel => "motorinden",
        EmissionSource.Gasoline => "benzinden",
        EmissionSource.Lpg => "LPG'den",
        EmissionSource.Coke => "kok kömüründen",
        EmissionSource.Lignite => "linyitten",
        _ => source.ToString()
    };

    /// <summary>En büyük karbon kaynağına göre sade yönlendirme.</summary>
    public static string CarbonGuidance(EmissionSource source) => source switch
    {
        EmissionSource.Electricity =>
            "Hangi makinenin ne kadar elektrik harcadığını ölçün ve boşta çalışmayı azaltın. Çatıya güneş paneli kurmanın size uygun olup olmadığını araştırın.",
        EmissionSource.NaturalGas or EmissionSource.Lpg or EmissionSource.Lignite =>
            "Kazanın ve fırının bakımını yaptırın, binayı ve boruları yalıtın. Fırından çıkan sıcak havayı tekrar kullanmanın yollarına bakın.",
        EmissionSource.Diesel or EmissionSource.Gasoline =>
            "Araç ve forklift kullanımını planlayın, yolları kısaltın, jeneratörü daha az çalıştırın.",
        EmissionSource.Coke =>
            "Önce ocağın doldurulmasını ve yanmayı iyileştirin. İleride elektrikli ocağa geçmenin size uygun olup olmadığını araştırın.",
        _ => "Bu kaynağın kullanımını takip edin ve nerede azaltabileceğinize bakın."
    };

    public const string CarbonLimitation =
        "Bu hesap bir ön tahmindir. Satın aldığınız malzeme ve taşıma gibi dolaylı kaynaklar dahil değildir. Resmi belge için uzman onayı gerekir.";

    public const string CarbonCbamNote =
        "Avrupalı alıcınız ürün başına karbon bilgisi isteyecek. Bu hesap o bilginin ilk adımıdır.";

    /// <summary>"2024 yılına göre karbonunuz yüzde 11 azaldı (91 ton daha az)."</summary>
    public static string CarbonComparison(int previousYear, decimal changeTonnes, decimal? changePercent)
    {
        if (changeTonnes == 0)
            return $"{previousYear} yılına göre karbonunuz değişmedi.";

        var direction = changeTonnes < 0 ? "azaldı" : "arttı";
        var amount = $"{Tonnes(Math.Abs(changeTonnes))} {(changeTonnes < 0 ? "daha az" : "daha fazla")}";
        return changePercent is null
            ? $"{previousYear} yılına göre karbonunuz {direction} ({amount})."
            : $"{previousYear} yılına göre karbonunuz {Percent(changePercent.Value)} {direction} ({amount}).";
    }
}
