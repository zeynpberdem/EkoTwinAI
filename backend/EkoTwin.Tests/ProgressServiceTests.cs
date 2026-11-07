using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using EkoTwin.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Tests;

/// <summary>
/// Gelişim takibi: değerlendirme karşılaştırması, otomatik öneri takibi, karbon trendi, hedef ve özet.
///
/// Ana senaryo (soru numarası → puan):
///   1. değerlendirme (Ocak):  S1-4 = 2 | S5-8 = 1 | S9-12 = 1       | S13-16 = 3
///      → Dijital 25, Enerji 0, Karbon 0, Tedarik 50, Genel 18,75 (Başlangıç)
///   2. değerlendirme (Temmuz): S1-4 = 2 | S5-8 = 3 | S9 = 4, S10-12 = 1 | S13-15 = 3, S16 = 2
///      → Dijital 25, Enerji 50, Karbon 18,75, Tedarik 43,75, Genel 34,38 (Farkındalık)
/// </summary>
public class ProgressServiceTests
{
    private static readonly DateTime January = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime July = new(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);

    private static ProgressService Service(AppDbContext db, DateTime? now = null) =>
        new(db, new FixedTimeProvider(new DateTimeOffset(now ?? new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc))));

    private static int First(int n) => n switch { <= 4 => 2, <= 12 => 1, _ => 3 };

    private static int Second(int n) => n switch
    {
        <= 4 => 2,
        <= 8 => 3,
        9 => 4,
        <= 12 => 1,
        16 => 2,
        _ => 3
    };

    private static async Task<(AppDbContext db, Company company, Guid first, Guid second)> ScenarioAsync()
    {
        var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var first = await TestDb.AddAssessmentAsync(db, company.Id, First, January);
        var second = await TestDb.AddAssessmentAsync(db, company.Id, Second, July);
        return (db, company, first, second);
    }

    // ===================== Karşılaştırma =====================

    [Fact]
    public async Task Compare_SkorVeSeviyeDegisimi()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var c = (await Service(db).CompareAsync(first, second))!;

        Assert.Equal(18.75m, c.Previous.TotalScore);
        Assert.Equal(34.38m, c.Current.TotalScore);
        Assert.Equal(15.63m, c.TotalChange);
        Assert.True(c.MaturityLevelChanged);
        Assert.Equal("Başlangıç → Farkındalık", c.MaturityChange);
        Assert.Equal(181, c.DaysBetween);
    }

    [Fact]
    public async Task Compare_BoyutDegisimleri_EnCokGelisenVeGeriyeGiden()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var c = (await Service(db).CompareAsync(first, second))!;
        decimal ChangeOf(Dimension d) => c.DimensionChanges.Single(x => x.Dimension == d).Change;

        Assert.Equal(0m, ChangeOf(Dimension.DigitalInfrastructure));
        Assert.Equal(50m, ChangeOf(Dimension.EnergyEfficiency));
        Assert.Equal(18.75m, ChangeOf(Dimension.CarbonManagement));
        Assert.Equal(-6.25m, ChangeOf(Dimension.SupplyChain));
        Assert.Equal(Dimension.EnergyEfficiency, c.MostImproved);
        Assert.Equal(Dimension.SupplyChain, c.MostDeclined);
    }

    [Fact]
    public async Task Compare_YalnizcaDegisenSorulariListeler()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var c = (await Service(db).CompareAsync(first, second))!;

        Assert.Equal(new[] { 5, 6, 7, 8, 9, 16 }, c.QuestionChanges.Select(q => q.QuestionNumber));
        var q9 = c.QuestionChanges.Single(q => q.QuestionNumber == 9);
        Assert.Equal((1, 4, 3), (q9.PreviousScore, q9.CurrentScore, q9.Change));
    }

    [Fact]
    public async Task Compare_SiraFarkEtmez_EskiOlanOnceki()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var c = (await Service(db).CompareAsync(second, first))!;

        Assert.Equal(first, c.Previous.Id);
        Assert.Equal(second, c.Current.Id);
    }

    // ===================== Öneri takibi (otomatik) =====================

    [Fact]
    public async Task OneriTakibi_SoruBazli_IyilestiVeHalaAcik()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var f = (await Service(db).CompareAsync(first, second))!.RecommendationFollowUp;
        FollowUpItemResponse Q(int n) => f.Items.Single(i => i.QuestionNumber == n && i.Status != FollowUpStatus.New);

        Assert.Equal(FollowUpStatus.Improved, Q(5).Status);     // 1 → 3
        Assert.Equal("Soru 5: 1/5 → 3/5", Q(5).Detail);
        Assert.Equal(FollowUpStatus.Improved, Q(9).Status);     // 1 → 4
        Assert.Equal(FollowUpStatus.StillOpen, Q(1).Status);    // 2 → 2
        Assert.Equal(FollowUpStatus.StillOpen, Q(10).Status);   // 1 → 1
        Assert.Equal("Hâlâ açık", Q(10).StatusName);
    }

    [Fact]
    public async Task OneriTakibi_BoyutBazli_EsikElliyeGore()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var f = (await Service(db).CompareAsync(first, second))!.RecommendationFollowUp;
        FollowUpItemResponse D(Dimension d) => f.Items.Single(i =>
            i.TriggerType == RecommendationTrigger.Dimension && i.Dimension == d && i.Status != FollowUpStatus.New);

        Assert.Equal(FollowUpStatus.Improved, D(Dimension.EnergyEfficiency).Status);       // 0 → 50
        Assert.Equal(FollowUpStatus.StillOpen, D(Dimension.CarbonManagement).Status);      // 0 → 18,75
        Assert.Equal(FollowUpStatus.StillOpen, D(Dimension.DigitalInfrastructure).Status); // 25 → 25
        Assert.Equal("Enerji Verimliliği skoru: 0 → 50", D(Dimension.EnergyEfficiency).Detail);
    }

    [Fact]
    public async Task OneriTakibi_GerileyenAlan_YeniOneriOlarakCikar()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var f = (await Service(db).CompareAsync(first, second))!.RecommendationFollowUp;
        var newItems = f.Items.Where(i => i.Status == FollowUpStatus.New).ToList();

        Assert.Equal(2, newItems.Count);
        Assert.Contains(newItems, i => i.QuestionNumber == 16 && i.Detail == "Soru 16: 3/5 → 2/5");
        Assert.Contains(newItems, i => i.TriggerType == RecommendationTrigger.Dimension && i.Dimension == Dimension.SupplyChain);
        Assert.All(newItems, i => Assert.False(i.WasTopPriority));
    }

    [Fact]
    public async Task OneriTakibi_Ozet_SayilarTutarli()
    {
        var (db, _, first, second) = await ScenarioAsync();
        await using var _db = db;

        var s = (await Service(db).CompareAsync(first, second))!.RecommendationFollowUp.Summary;

        // Önceki: 12 aksiyon (S1-12) + 3 stratejik uyarı (Dijital, Enerji, Karbon) = 15
        // İyileşen: S5-8 (4) + S9 (1) + Enerji uyarısı (1) = 6
        Assert.Equal(15, s.PreviousCount);
        Assert.Equal(6, s.ImprovedCount);
        Assert.Equal(9, s.StillOpenCount);
        Assert.Equal(2, s.NewCount);
        Assert.Equal(3, s.TopPriorityCount);
        Assert.Contains("Önceki 15 öneriden 6 tanesinde iyileşme var", s.Summary);
        Assert.Contains("2 yeni öneri çıktı", s.Summary);
    }

    [Fact]
    public async Task Compare_AyniDegerlendirme_VeyaFarkliFirma_Gecersiz_YoksaNull()
    {
        var (db, _, first, _) = await ScenarioAsync();
        await using var _db = db;
        var other = await TestDb.AddCompanyAsync(db, "Başka Firma");
        var foreign = await TestDb.AddAssessmentAsync(db, other.Id, _ => 3, July);
        var service = Service(db);

        await Assert.ThrowsAsync<ProgressValidationException>(() => service.CompareAsync(first, first));
        await Assert.ThrowsAsync<ProgressValidationException>(() => service.CompareAsync(first, foreign));
        Assert.Null(await service.CompareAsync(first, Guid.NewGuid()));
    }

    // ===================== Gelişim özeti =====================

    [Fact]
    public async Task Progress_FirmaYoksaNull_DegerlendirmeYoksaYonlendirir()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        Assert.Null(await service.GetProgressAsync(Guid.NewGuid()));

        var p = (await service.GetProgressAsync(company.Id))!;
        Assert.Equal(0, p.AssessmentCount);
        Assert.Null(p.LatestComparison);
        Assert.Null(p.DaysSinceLastAssessment);
        Assert.False(p.ReassessmentDue);
        Assert.Contains(p.Highlights, h => h.Contains("ilk değerlendirme"));
        Assert.Contains(p.Highlights, h => h.Contains("Karbon ayak izinizi hesaplayarak"));
    }

    [Fact]
    public async Task Progress_TekDegerlendirme_KarsilastirmaYok()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        await TestDb.AddAssessmentAsync(db, company.Id, First, July);

        var p = (await Service(db).GetProgressAsync(company.Id))!;

        Assert.Equal(1, p.AssessmentCount);
        Assert.Null(p.LatestComparison);
        Assert.Contains(p.Highlights, h => h.Contains("6 ay sonra"));
    }

    [Fact]
    public async Task Progress_SonIkiDegerlendirmeyiKarsilastirir_ZamanSirasiEskidenYeniye()
    {
        var (db, company, first, second) = await ScenarioAsync();
        await using var _db = db;
        var third = await TestDb.AddAssessmentAsync(db, company.Id, _ => 4, new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc));

        var p = (await Service(db).GetProgressAsync(company.Id))!;

        Assert.Equal(new[] { first, second, third }, p.Assessments.Select(a => a.Id));
        Assert.Equal(second, p.LatestComparison!.Previous.Id);
        Assert.Equal(third, p.LatestComparison.Current.Id);
    }

    [Fact]
    public async Task Progress_OzetMaddeleri()
    {
        var (db, company, _, _) = await ScenarioAsync();
        await using var _db = db;

        var p = (await Service(db).GetProgressAsync(company.Id))!;

        Assert.Contains("Genel skorunuz 18,75 → 34,38 (+15,63).", p.Highlights);
        Assert.Contains("Olgunluk seviyeniz yükseldi: Başlangıç → Farkındalık.", p.Highlights);
        Assert.Contains("En çok gelişen boyut: Enerji Verimliliği (+50).", p.Highlights);
        Assert.Contains("Dikkat: Tedarik Zinciri 6,25 puan geriledi.", p.Highlights);
    }

    [Theory]
    [InlineData(179, false)]
    [InlineData(180, true)]
    public async Task Progress_YenidenDegerlendirmeHatirlatmasi_180Gun(int daysLater, bool expectDue)
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        await TestDb.AddAssessmentAsync(db, company.Id, First, January);

        var p = (await Service(db, January.AddDays(daysLater)).GetProgressAsync(company.Id))!;

        Assert.Equal(daysLater, p.DaysSinceLastAssessment);
        Assert.Equal(expectDue, p.ReassessmentDue);
        Assert.Equal(expectDue, p.Highlights.Any(h => h.Contains("yenilemeniz önerilir")));
    }

    [Fact]
    public async Task Progress_KarbonTrendi_BazYilaGoreDegisim()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        await TestDb.AddFootprintAsync(db, company.Id, 2023, 1000m, 20m);
        await TestDb.AddFootprintAsync(db, company.Id, 2025, 850m, 15m);
        await TestDb.AddFootprintAsync(db, company.Id, 2024, 900m, 18m);

        var carbon = (await Service(db).GetProgressAsync(company.Id))!.Carbon;

        Assert.Equal(new[] { 2023, 2024, 2025 }, carbon.Series.Select(s => s.Year));
        Assert.Equal(2023, carbon.BaseYear);
        Assert.Equal(2025, carbon.LatestYear);
        Assert.Equal(-15.0m, carbon.ChangeSinceBasePercent);
        Assert.Equal(-25.0m, carbon.PerEmployeeChangeSinceBasePercent);
        Assert.Null(carbon.Target);
    }

    // ===================== Karbon hedefi =====================

    [Fact]
    public async Task Hedef_BazYilKaydiOlmadan_Kurulamaz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);

        var ex = await Assert.ThrowsAsync<ProgressValidationException>(() => Service(db).SetTargetAsync(company.Id,
            new CarbonTargetRequest { BaseYear = 2024, TargetYear = 2030, ReductionPercent = 20m }));
        Assert.Contains(ex.Errors, e => e.Contains("baz yıl (2024)"));
    }

    [Fact]
    public async Task Hedef_HedefYiliBazdanOnce_Gecersiz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        await TestDb.AddFootprintAsync(db, company.Id, 2024, 1000m);

        var ex = await Assert.ThrowsAsync<ProgressValidationException>(() => Service(db).SetTargetAsync(company.Id,
            new CarbonTargetRequest { BaseYear = 2024, TargetYear = 2024, ReductionPercent = 20m }));
        Assert.Contains(ex.Errors, e => e.Contains("baz yıldan sonra"));
    }

    [Fact]
    public async Task Hedef_KurGuncelleOkuSil_FirmaBasinaTekHedef()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        await TestDb.AddFootprintAsync(db, company.Id, 2023, 1000m);
        await TestDb.AddFootprintAsync(db, company.Id, 2025, 900m);
        var service = Service(db);

        var created = await service.SetTargetAsync(company.Id,
            new CarbonTargetRequest { BaseYear = 2023, TargetYear = 2030, ReductionPercent = 20m });
        Assert.Equal(TargetStatus.OnTrack, created.Status); // 2 yılda %10, beklenen %5,7
        Assert.Null(created.UpdatedAt);

        var updated = await service.SetTargetAsync(company.Id,
            new CarbonTargetRequest { BaseYear = 2023, TargetYear = 2030, ReductionPercent = 50m });
        Assert.Equal(TargetStatus.Behind, updated.Status); // beklenen %14,3
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal(1, await db.CarbonTargets.CountAsync());

        var progress = (await service.GetProgressAsync(company.Id))!;
        Assert.Equal(updated.Summary, progress.Carbon.Target!.Summary);
        Assert.Contains(updated.Summary, progress.Highlights);

        Assert.True(await service.DeleteTargetAsync(company.Id));
        Assert.Null(await service.GetTargetAsync(company.Id));
        Assert.False(await service.DeleteTargetAsync(company.Id));
    }

    [Fact]
    public async Task Hedef_FirmaYoksa_KeyNotFound()
    {
        await using var db = TestDb.Create();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(db).GetTargetAsync(Guid.NewGuid()));
    }
}
