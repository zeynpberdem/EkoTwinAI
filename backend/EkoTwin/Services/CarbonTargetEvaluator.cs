using EkoTwin.Dtos;
using EkoTwin.Models;
using static EkoTwin.Services.TurkishFormat;

namespace EkoTwin.Services;

/// <summary>
/// Karbon azaltım hedefinin ilerlemesi. Veritabanından bağımsızdır.
///
///   Hedef emisyon        = Baz yıl emisyonu × (1 − hedef % / 100)
///   Gerçekleşen azalma % = (Baz − Son yıl) / Baz × 100
///   İlerleme %           = Gerçekleşen azalma / Hedef azalma × 100
///   Beklenen azalma %    = Hedef % × (geçen yıl / toplam hedef süresi)   ← doğrusal yol
///   Durum                = Ulaşıldı (gerçekleşen ≥ hedef) · Yolunda (gerçekleşen ≥ beklenen) · Geride
///   Tahmini yıl          = Bugüne kadarki yıllık ortalama hızla hedefin tamamlanacağı yıl
///
/// Özet iki dilde üretilir: teknik (Summary) ve sade (SimpleSummary; KOBİ sahibine gösterilir).
/// </summary>
public static class CarbonTargetEvaluator
{
    public record YearTotal(int Year, decimal TotalTonnes);

    public static string StatusName(TargetStatus status) => status switch
    {
        TargetStatus.BaseYearMissing => "Baz yıl verisi yok",
        TargetStatus.NoDataAfterBaseYear => "Baz yıldan sonra veri yok",
        TargetStatus.Achieved => "Hedefe ulaşıldı",
        TargetStatus.OnTrack => "Yolunda",
        TargetStatus.Behind => "Geride",
        _ => status.ToString()
    };

    public static string SimpleStatusName(TargetStatus status) => status switch
    {
        TargetStatus.BaseYearMissing => "Başlangıç yılı bilgisi eksik",
        TargetStatus.NoDataAfterBaseYear => "Henüz yeni yıl bilgisi yok",
        TargetStatus.Achieved => "Hedefe ulaştınız",
        TargetStatus.OnTrack => "İyi gidiyorsunuz",
        TargetStatus.Behind => "Geride kalıyorsunuz",
        _ => status.ToString()
    };

    public static CarbonTargetResponse Evaluate(CarbonTarget target, IEnumerable<YearTotal> footprints)
    {
        var byYear = footprints.ToDictionary(f => f.Year, f => f.TotalTonnes);
        var p = target.ReductionPercent;
        var by = target.BaseYear;

        CarbonTargetResponse Build(TargetStatus status, string summary, string simpleSummary, decimal? baseT = null,
            decimal? targetT = null, int? latestYear = null, decimal? latestT = null, decimal? achieved = null,
            decimal? progress = null, decimal? expected = null, int? projected = null) => new(
            target.Id, target.BaseYear, target.TargetYear, p, baseT, targetT, latestYear, latestT,
            achieved, progress, expected, projected, status, StatusName(status), summary,
            SimpleStatusName(status), simpleSummary, target.CreatedAt, target.UpdatedAt);

        if (!byYear.TryGetValue(by, out var baseTonnes) || baseTonnes <= 0)
            return Build(TargetStatus.BaseYearMissing,
                $"Baz yıl ({by}) için karbon kaydı bulunamadı; hedef ilerlemesi hesaplanamıyor.",
                $"Başlangıç yılı ({by}) için karbon hesabı bulunamadı. Önce o yılın karbonunu girin.");

        var targetTonnes = Round(baseTonnes * (1 - p / 100m), 3);

        var latest = byYear.Where(kv => kv.Key > by).OrderByDescending(kv => kv.Key).FirstOrDefault();
        if (latest.Key == 0)
            return Build(TargetStatus.NoDataAfterBaseYear,
                $"Hedef: {target.TargetYear} yılına kadar emisyonu {Number(baseTonnes, "0.#")} t'dan " +
                $"{Number(targetTonnes, "0.#")} t'a indirmek. İlerlemeyi görmek için {by + 1} ve " +
                "sonraki yılların karbon hesabını girin.",
                $"Hedefiniz: {target.TargetYear} yılına kadar karbonunuzu yaklaşık {Tons(baseTonnes)}dan {Tons(targetTonnes)}a " +
                $"indirmek. İlerlemeyi görmek için {by + 1} ve sonraki yılların karbonunu girin.",
                baseTonnes, targetTonnes);

        var latestYear = latest.Key;
        var latestTonnes = latest.Value;

        var achieved = Round((baseTonnes - latestTonnes) / baseTonnes * 100m, 1);
        var progress = Round(achieved / p * 100m, 1);

        var elapsed = latestYear - by;
        var span = target.TargetYear - by;
        var expected = Round(p * Math.Min(elapsed, span) / span, 1);

        var status = achieved >= p ? TargetStatus.Achieved
            : achieved >= expected ? TargetStatus.OnTrack
            : TargetStatus.Behind;

        int? projected = null;
        if (status != TargetStatus.Achieved && achieved > 0)
        {
            var annualRate = achieved / elapsed;
            projected = by + (int)Math.Ceiling(p / annualRate);
        }

        var summary = status switch
        {
            TargetStatus.Achieved =>
                $"Hedefinize ulaştınız: {latestYear} itibarıyla baz yıla göre %{Number(achieved, "0.#")} azalma (hedef %{Number(p)}).",
            TargetStatus.OnTrack =>
                $"Hedefinize doğru yolundasınız: baz yıla göre %{Number(achieved, "0.#")} azalma " +
                $"(bu tarihe kadar beklenen %{Number(expected, "0.#")})." +
                (projected is null ? "" : $" Bu hızla hedefe {projected} yılında ulaşırsınız."),
            _ when achieved > 0 =>
                $"Hedefin gerisindesiniz: baz yıla göre %{Number(achieved, "0.#")} azalma (beklenen %{Number(expected, "0.#")}). " +
                $"Bu hızla hedefe {projected} yılında ulaşırsınız; hedef yıl {target.TargetYear}.",
            _ when achieved < 0 =>
                $"Emisyonunuz baz yıla göre %{Number(Math.Abs(achieved), "0.#")} arttı; " +
                $"{target.TargetYear} hedefi için azaltım adımları gerekli.",
            _ =>
                $"Emisyonunuz baz yıla göre değişmedi; {target.TargetYear} hedefi için azaltım adımları gerekli."
        };

        var change = PlainLanguage.Percent(achieved);
        var simpleSummary = status switch
        {
            TargetStatus.Achieved =>
                $"Tebrikler, hedefinize ulaştınız. {by} yılından bu yana karbonunuz {change} azaldı.",
            TargetStatus.OnTrack =>
                $"Hedefinize doğru iyi gidiyorsunuz. {by} yılından bu yana karbonunuz {change} azaldı." +
                (projected is null ? "" : $" Bu hızla hedefe {projected} yılında ulaşırsınız."),
            _ when achieved > 0 =>
                $"Hedefin gerisinde kalıyorsunuz. {by} yılından bu yana karbonunuz {change} azaldı, ama daha hızlı " +
                $"azalması gerekiyor. Bu hızla hedefe {projected} yılında ulaşırsınız; hedefiniz {target.TargetYear} yılı.",
            _ when achieved < 0 =>
                $"Karbonunuz {by} yılından bu yana {change} arttı. Hedefe ulaşmak için azaltma adımlarına başlamanız gerekiyor.",
            _ =>
                $"Karbonunuz {by} yılından bu yana değişmedi. Hedefe ulaşmak için azaltma adımlarına başlamanız gerekiyor."
        };

        return Build(status, summary, simpleSummary, baseTonnes, targetTonnes, latestYear, latestTonnes,
            achieved, progress, expected, projected);
    }

    /// <summary>Yuvarlanmış ton ("740 ton"); cümlede "-dan" / "-a" eki doğrudan eklenir ("740 tondan", "592 tona").</summary>
    private static string Tons(decimal tonnes) => PlainLanguage.Tonnes(tonnes);

    private static decimal Round(decimal value, int digits) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
}
