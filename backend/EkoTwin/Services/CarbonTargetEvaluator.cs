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

    public static CarbonTargetResponse Evaluate(CarbonTarget target, IEnumerable<YearTotal> footprints)
    {
        var byYear = footprints.ToDictionary(f => f.Year, f => f.TotalTonnes);
        var p = target.ReductionPercent;

        CarbonTargetResponse Build(TargetStatus status, string summary, decimal? baseT = null, decimal? targetT = null,
            int? latestYear = null, decimal? latestT = null, decimal? achieved = null, decimal? progress = null,
            decimal? expected = null, int? projected = null) => new(
            target.Id, target.BaseYear, target.TargetYear, p, baseT, targetT, latestYear, latestT,
            achieved, progress, expected, projected, status, StatusName(status), summary,
            target.CreatedAt, target.UpdatedAt);

        if (!byYear.TryGetValue(target.BaseYear, out var baseTonnes) || baseTonnes <= 0)
            return Build(TargetStatus.BaseYearMissing,
                $"Baz yıl ({target.BaseYear}) için karbon kaydı bulunamadı; hedef ilerlemesi hesaplanamıyor.");

        var targetTonnes = Round(baseTonnes * (1 - p / 100m), 3);

        var latest = byYear.Where(kv => kv.Key > target.BaseYear).OrderByDescending(kv => kv.Key).FirstOrDefault();
        if (latest.Key == 0)
            return Build(TargetStatus.NoDataAfterBaseYear,
                $"Hedef: {target.TargetYear} yılına kadar emisyonu {Number(baseTonnes, "0.#")} t'dan " +
                $"{Number(targetTonnes, "0.#")} t'a indirmek. İlerlemeyi görmek için {target.BaseYear + 1} ve " +
                "sonraki yılların karbon hesabını girin.",
                baseTonnes, targetTonnes);

        var latestYear = latest.Key;
        var latestTonnes = latest.Value;

        var achieved = Round((baseTonnes - latestTonnes) / baseTonnes * 100m, 1);
        var progress = Round(achieved / p * 100m, 1);

        var elapsed = latestYear - target.BaseYear;
        var span = target.TargetYear - target.BaseYear;
        var expected = Round(p * Math.Min(elapsed, span) / span, 1);

        var status = achieved >= p ? TargetStatus.Achieved
            : achieved >= expected ? TargetStatus.OnTrack
            : TargetStatus.Behind;

        int? projected = null;
        if (status != TargetStatus.Achieved && achieved > 0)
        {
            var annualRate = achieved / elapsed;
            projected = target.BaseYear + (int)Math.Ceiling(p / annualRate);
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

        return Build(status, summary, baseTonnes, targetTonnes, latestYear, latestTonnes,
            achieved, progress, expected, projected);
    }

    private static decimal Round(decimal value, int digits) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
}
