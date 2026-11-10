using System.Globalization;
using System.Text.RegularExpressions;

namespace EkoTwin.Services.Narrative;

/// <summary>
/// Yapay zekâ metninin otomatik kontrolü. Kontrolden geçemeyen metin kullanıcıya gösterilmez;
/// yerine kural tabanlı şablon kullanılır.
///
///   1) Rakam:        metindeki her sayı bilgi kartında geçmeli (yapay zekâ rakam uyduramaz)
///   2) Jargon:       teknik terim yok
///   3) Yasak ifade:  vaat / kesinlik / hukuki tavsiye yok
///   4) Uzunluk:      80-350 kelime
///   5) Okunabilirlik: Ateşman ≥ 50 (en az "orta güçlükte")
/// </summary>
public static partial class NarrativeValidator
{
    public const int MinWords = 80;
    public const int MaxWords = 350;
    public const decimal MinReadability = 50m;

    /// <summary>Mektupta geçmemesi gereken teknik terimler.</summary>
    public static readonly string[] Jargon =
    {
        "ERP", "CRM", "LCA", "CBAM", "SKDM", "IoT", "Kapsam 1", "Kapsam 2", "Kapsam 3", "emisyon", "CO2",
        "firewall", "lojistik", "optimiz", "entegre", "sürdürülebilir", "yetkinlik", "etüt", "otomasyon",
        "operasyon", "darboğaz", "skor", "baz yıl"
    };

    /// <summary>Vaat veya kesinlik bildiren ifadeler.</summary>
    public static readonly string[] ForbiddenPhrases =
    {
        "kesinlikle", "garanti", "mutlaka kazan", "ceza almazsınız", "yasal olarak", "hukuken", "%100"
    };

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    [GeneratedRegex(@"\d+(?:[.,]\d+)*")]
    private static partial Regex NumberRegex();

    public record Result(bool IsValid, IReadOnlyList<string> Errors);

    public static Result Validate(string text, IReadOnlyList<string> facts)
    {
        var errors = new List<string>();

        // 1) Rakam kontrolü
        var allowed = Numbers(string.Join("\n", facts)).ToHashSet();
        var invented = Numbers(text).Where(n => !allowed.Contains(n)).Distinct().ToList();
        if (invented.Count > 0)
            errors.Add($"Metinde bilgi kartında olmayan sayı var: {string.Join(", ", invented.Select(n => n.ToString(Tr)))}");

        // 2) Jargon
        var jargon = Jargon.Where(t => Tr.CompareInfo.IndexOf(text, t, CompareOptions.IgnoreCase) >= 0).ToList();
        if (jargon.Count > 0)
            errors.Add($"Metinde teknik terim var: {string.Join(", ", jargon)}");

        // 3) Yasak ifadeler
        var forbidden = ForbiddenPhrases.Where(p => Tr.CompareInfo.IndexOf(text, p, CompareOptions.IgnoreCase) >= 0).ToList();
        if (forbidden.Count > 0)
            errors.Add($"Metinde vaat veya kesinlik bildiren ifade var: {string.Join(", ", forbidden)}");

        // 4) Uzunluk
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words < MinWords || words > MaxWords)
            errors.Add($"Metin uzunluğu uygun değil: {words} kelime (beklenen {MinWords}-{MaxWords}).");

        // 5) Okunabilirlik
        var readability = Readability.Atesman(text);
        if (readability.Score < MinReadability)
            errors.Add($"Metin zor okunuyor: Ateşman {readability.Score.ToString(Tr)} ({readability.Level}).");

        return new Result(errors.Count == 0, errors);
    }

    /// <summary>Biçimlendirme işaretlerini temizler (yıldız, kare, satır başı tireleri).</summary>
    public static string Clean(string text)
    {
        var lines = text.Replace("\r", "")
            .Split('\n')
            .Select(l => l.Replace("**", "").Replace("*", "").TrimStart('#', ' ', '-', '•').TrimEnd());
        return string.Join("\n", lines).Trim();
    }

    /// <summary>
    /// Metindeki sayılar (Türkçe yazımla): "1.210" → 1210, "4,8" → 4.8, "2027" → 2027, "%66" → 66.
    /// Nokta, arkasında tam 3 rakam varsa binlik ayırıcı sayılır.
    /// </summary>
    public static IEnumerable<decimal> Numbers(string text)
    {
        foreach (Match m in NumberRegex().Matches(text))
        {
            var raw = m.Value;
            string normalized;
            if (raw.Contains(','))
                normalized = raw.Replace(".", "").Replace(',', '.');
            else if (Regex.IsMatch(raw, @"^\d{1,3}(\.\d{3})+$"))
                normalized = raw.Replace(".", "");
            else
                normalized = raw;

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                yield return value;
        }
    }
}
