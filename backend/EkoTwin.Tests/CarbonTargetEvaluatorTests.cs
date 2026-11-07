using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using Y = EkoTwin.Services.CarbonTargetEvaluator.YearTotal;

namespace EkoTwin.Tests;

/// <summary>
/// Karbon hedefi ilerlemesi. Senaryo: 2022 baz yılı 1000 t, 2030'a kadar %20 azaltma (hedef 800 t).
/// Doğrusal yolda yılda %2,5 azalma beklenir.
/// </summary>
public class CarbonTargetEvaluatorTests
{
    private static readonly CarbonTarget Target = new()
    {
        Id = Guid.NewGuid(), BaseYear = 2022, TargetYear = 2030, ReductionPercent = 20m
    };

    private static CarbonTargetResponse Eval(params Y[] years) => CarbonTargetEvaluator.Evaluate(Target, years);

    [Fact]
    public void BazYilKaydiYok_BaseYearMissing()
    {
        var r = Eval(new Y(2023, 900m));

        Assert.Equal(TargetStatus.BaseYearMissing, r.Status);
        Assert.Null(r.AchievedReductionPercent);
        Assert.Contains("2022", r.Summary);
    }

    [Fact]
    public void BazYildanSonraVeriYok_HedefTonajiniGosterir()
    {
        var r = Eval(new Y(2021, 1200m), new Y(2022, 1000m));

        Assert.Equal(TargetStatus.NoDataAfterBaseYear, r.Status);
        Assert.Equal(1000m, r.BaseYearTonnes);
        Assert.Equal(800m, r.TargetTonnes);
        Assert.Contains("2023", r.Summary);
    }

    [Fact]
    public void BeklenenKadarAzalma_Yolunda_TahminiYilHedefYili()
    {
        // 2 yılda %5 azalma = beklenen %5 → yolunda; yılda %2,5 hızla 8 yılda → 2030
        var r = Eval(new Y(2022, 1000m), new Y(2024, 950m));

        Assert.Equal(TargetStatus.OnTrack, r.Status);
        Assert.Equal(5.0m, r.AchievedReductionPercent);
        Assert.Equal(5.0m, r.ExpectedReductionPercent);
        Assert.Equal(25.0m, r.ProgressPercent);
        Assert.Equal(2030, r.ProjectedYear);
        Assert.Equal(2024, r.LatestYear);
        Assert.Contains("yolundasınız", r.Summary);
    }

    [Fact]
    public void BeklenendenAzAzalma_Geride_TahminiYilGecikir()
    {
        // 2 yılda %2 → yılda %1 → %20 için 20 yıl → 2042
        var r = Eval(new Y(2022, 1000m), new Y(2024, 980m));

        Assert.Equal(TargetStatus.Behind, r.Status);
        Assert.Equal(2.0m, r.AchievedReductionPercent);
        Assert.Equal(2042, r.ProjectedYear);
        Assert.Contains("gerisindesiniz", r.Summary);
    }

    [Fact]
    public void HedefKadarAzalma_Ulasildi()
    {
        var r = Eval(new Y(2022, 1000m), new Y(2024, 950m), new Y(2026, 790m));

        Assert.Equal(TargetStatus.Achieved, r.Status);
        Assert.Equal(21.0m, r.AchievedReductionPercent);
        Assert.Equal(2026, r.LatestYear); // en son yıl kullanılır
        Assert.Null(r.ProjectedYear);
    }

    [Fact]
    public void EmisyonArtmis_Geride_TahminYok()
    {
        var r = Eval(new Y(2022, 1000m), new Y(2024, 1100m));

        Assert.Equal(TargetStatus.Behind, r.Status);
        Assert.Equal(-10.0m, r.AchievedReductionPercent);
        Assert.Null(r.ProjectedYear);
        Assert.Contains("arttı", r.Summary);
    }

    [Fact]
    public void HedefYiliGecmis_BeklenenTamHedefOlur()
    {
        // 2033'te %10 azalma: beklenen artık %20 (hedef yılı geçti) → geride
        var r = Eval(new Y(2022, 1000m), new Y(2033, 900m));

        Assert.Equal(20.0m, r.ExpectedReductionPercent);
        Assert.Equal(TargetStatus.Behind, r.Status);
    }
}
