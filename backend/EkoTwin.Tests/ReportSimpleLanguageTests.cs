using System.Globalization;
using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using EkoTwin.Tests.Helpers;
using Y = EkoTwin.Services.CarbonTargetEvaluator.YearTotal;

namespace EkoTwin.Tests;

/// <summary>
/// Sade dil 3/3: karbon, Avrupa (CBAM) ve gelişim raporlarının KOBİ sahibine gösterilen cümleleri.
/// Beklenen cümleler birebir yazılır; böylece metin değişirse test bunu görünür kılar.
/// </summary>
public class ReportSimpleLanguageTests
{
    /// <summary>
    /// Rapor cümlelerinde geçmemesi gereken teknik ifadeler. "Kapsam" tek başına yasak değildir
    /// ("kuralın kapsamında" günlük dildir); GHG terimleri "Kapsam 1/2/3" yasaktır.
    /// </summary>
    private static readonly string[] ReportJargon = QuestionSeedTests.Jargon
        .Where(t => t != "Kapsam")
        .Concat(new[] { "Kapsam 1", "Kapsam 2", "Kapsam 3", "CO2", "baz yıl", "skor", "darboğaz", "muafiyet", "%" })
        .ToArray();

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeProvider Clock = FixedTimeProvider.At(2026);

    private static List<string> JargonIn(IEnumerable<string?> texts) =>
        (from t in texts.OfType<string>()
         from term in ReportJargon
         where Tr.CompareInfo.IndexOf(t, term, CompareOptions.IgnoreCase) >= 0
         select $"\"{term}\" → {t}").ToList();

    private static CarbonFootprintRequest Request(int year, params (EmissionSource s, decimal q)[] entries) => new()
    {
        Year = year,
        Entries = entries.Select(e => new CarbonEntryRequest { Source = e.s, Quantity = e.q }).ToList()
    };

    // ===================== Yuvarlama kuralları =====================

    [Theory]
    [InlineData(740.366, "740 ton")]
    [InlineData(1209.96, "1.210 ton")]
    [InlineData(19.372, "19 ton")]
    [InlineData(4.83, "4,8 ton")]
    [InlineData(0.04, "0,1 tondan az")]
    [InlineData(0, "0 ton")]
    public void Ton_Yuvarlanir(double tonnes, string expected) =>
        Assert.Equal(expected, PlainLanguage.Tonnes((decimal)tonnes));

    [Theory]
    [InlineData(10.9, "yüzde 11")]
    [InlineData(-20, "yüzde 20")]
    [InlineData(83.2, "yüzde 83")]
    public void Yuzde_TamSayiVeKelimeyle(double percent, string expected) =>
        Assert.Equal(expected, PlainLanguage.Percent((decimal)percent));

    // ===================== Karbon raporu =====================

    [Fact]
    public async Task KarbonRaporu_SadeOzet()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db); // 45 çalışan
        var r = await new CarbonFootprintService(db, Clock).CreateAsync(company.Id, Request(2025,
            (EmissionSource.Electricity, 200_000m),   // 95,6 t
            (EmissionSource.NaturalGas, 10_000m)));   // 19,372 t

        Assert.Equal("Firmanızın 2025 yılı karbonu: yaklaşık 115 ton", r.Simple.Headline);
        Assert.Equal("Kendi yaktığınız yakıtlardan: 19 ton", r.Simple.OwnFuels);
        Assert.Equal("Kullandığınız elektrikten: 96 ton", r.Simple.Electricity);
        Assert.Equal("Çalışan başına: 2,6 ton", r.Simple.PerEmployee);
        Assert.Equal("Karbonunuz en çok elektrikten geliyor (yüzde 83).", r.Simple.LargestSource);
        Assert.StartsWith("Hangi makinenin", r.Simple.Guidance);
        Assert.Null(r.Simple.Comparison);
        Assert.Null(r.Simple.CbamNote);
        Assert.Equal("Kullandığınız elektrik", r.Items.First().SimpleScopeName);
        Assert.Equal("96 ton", r.Items.First().SimpleTonnes);

        // Teknik değerler korunur
        Assert.Equal(114.972m, r.TotalTonnes);
    }

    [Fact]
    public async Task KarbonRaporu_OncekiYilKarsilastirmasiVeAvrupaNotu()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db, exportsToEu: true, cbamProductGroup: CbamProductGroup.IronAndSteel);
        var service = new CarbonFootprintService(db, Clock);

        await service.CreateAsync(company.Id, Request(2024, (EmissionSource.Electricity, 100_000m))); // 47,8 t
        var r = await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 80_000m))); // 38,24 t

        Assert.Equal("2024 yılına göre karbonunuz yüzde 20 azaldı (9,6 ton daha az).", r.Simple.Comparison);
        Assert.Equal(PlainLanguage.CarbonCbamNote, r.Simple.CbamNote);
    }

    [Fact]
    public void KarbonKarsilastirmasi_ArtisVeDegisimYok()
    {
        Assert.Equal("2024 yılına göre karbonunuz yüzde 13 arttı (10 ton daha fazla).",
            PlainLanguage.CarbonComparison(2024, 10m, 12.5m));
        Assert.Equal("2024 yılına göre karbonunuz değişmedi.", PlainLanguage.CarbonComparison(2024, 0m, 0m));
    }

    // ===================== Avrupa (CBAM) =====================

    [Fact]
    public void Cbam_SadeSeviyeVeOzet()
    {
        var high = CbamRiskEvaluator.Evaluate(true, CbamProductGroup.IronAndSteel, 10m);
        var none = CbamRiskEvaluator.Evaluate(true, CbamProductGroup.None, 10m);
        var notExporting = CbamRiskEvaluator.Evaluate(false, CbamProductGroup.Aluminium, 10m);
        var ready = CbamRiskEvaluator.Evaluate(true, CbamProductGroup.Cement, 90m);

        Assert.Equal("Yüksek risk", high.SimpleRiskLevel);
        Assert.StartsWith("Avrupa'ya sattığınız demir-çelik ürünleri için karbon bilgisi istenecek ve şu an hazır değilsiniz.", high.SimpleSummary);
        Assert.Equal(high.Actions.Count, high.SimpleActions.Count);

        Assert.Equal("Etkilenmiyorsunuz", none.SimpleRiskLevel);
        Assert.Equal("Şimdilik etkilenmiyorsunuz", notExporting.SimpleRiskLevel);
        Assert.Contains("(alüminyum)", notExporting.SimpleSummary);
        Assert.Equal("Hazırsınız", ready.SimpleRiskLevel);
    }

    // ===================== Karbon hedefi =====================

    private static readonly CarbonTarget Target = new() { Id = Guid.NewGuid(), BaseYear = 2022, TargetYear = 2030, ReductionPercent = 20m };

    [Fact]
    public void Hedef_SadeOzetler()
    {
        var onTrack = CarbonTargetEvaluator.Evaluate(Target, new[] { new Y(2022, 1000m), new Y(2024, 950m) });
        var noData = CarbonTargetEvaluator.Evaluate(Target, new[] { new Y(2022, 1000m) });
        var up = CarbonTargetEvaluator.Evaluate(Target, new[] { new Y(2022, 1000m), new Y(2024, 1100m) });
        var done = CarbonTargetEvaluator.Evaluate(Target, new[] { new Y(2022, 1000m), new Y(2026, 790m) });

        Assert.Equal("İyi gidiyorsunuz", onTrack.SimpleStatusName);
        Assert.Equal("Hedefinize doğru iyi gidiyorsunuz. 2022 yılından bu yana karbonunuz yüzde 5 azaldı. " +
                     "Bu hızla hedefe 2030 yılında ulaşırsınız.", onTrack.SimpleSummary);
        Assert.Equal("Hedefiniz: 2030 yılına kadar karbonunuzu yaklaşık 1.000 tondan 800 tona indirmek. " +
                     "İlerlemeyi görmek için 2023 ve sonraki yılların karbonunu girin.", noData.SimpleSummary);
        Assert.StartsWith("Karbonunuz 2022 yılından bu yana yüzde 10 arttı.", up.SimpleSummary);
        Assert.Equal("Hedefe ulaştınız", done.SimpleStatusName);
    }

    // ===================== Gelişim raporu =====================

    private static readonly DateTime January = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime July = new(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>ProgressServiceTests ile aynı senaryo (Genel 18,75 → 34,38).</summary>
    private static async Task<(AppDbContext db, Guid company, ProgressResponse progress)> ProgressScenarioAsync()
    {
        var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        await TestDb.AddAssessmentAsync(db, company.Id, n => n switch { <= 4 => 2, <= 12 => 1, _ => 3 }, January);
        await TestDb.AddAssessmentAsync(db, company.Id, n => n switch
        {
            <= 4 => 2, <= 8 => 3, 9 => 4, <= 12 => 1, 16 => 2, _ => 3
        }, July);
        var service = new ProgressService(db, FixedTimeProvider.At(2026, 8, 1));
        return (db, company.Id, (await service.GetProgressAsync(company.Id))!);
    }

    [Fact]
    public async Task Gelisim_SadeOzetMaddeleri()
    {
        var (db, _, p) = await ProgressScenarioAsync();
        await using var _db = db;

        Assert.Contains("Genel puanınız yükseldi: 19 → 34 (100 üzerinden).", p.SimpleHighlights);
        Assert.Contains("Seviyeniz yükseldi: Yolun başında → Farkında.", p.SimpleHighlights);
        Assert.Contains("En çok ilerlediğiniz konu: Enerji tasarrufu.", p.SimpleHighlights);
        Assert.Contains("Dikkat: Tedarikçiler ve malzeme konusunda geriye gittiniz.", p.SimpleHighlights);
        Assert.Contains("Geçen sefer önerilen 15 işten 6 tanesinde ilerleme var.", p.SimpleHighlights.Single(h => h.StartsWith("Geçen sefer")));
        Assert.Contains("Karbonunuzu hesaplayarak takibe başlayın.", p.SimpleHighlights);
    }

    [Fact]
    public async Task Gelisim_CevapDegisimiSecilenCumlelerleGosterilir()
    {
        var (db, _, p) = await ProgressScenarioAsync();
        await using var _db = db;
        var c = p.LatestComparison!;

        var q5 = c.RecommendationFollowUp.Items.Single(i => i.QuestionNumber == 5 && i.Status == FollowUpStatus.Improved);
        Assert.Equal("Önce: “Hayır, sadece faturayı ödüyoruz” → Şimdi: “Bazı bölümlerde ayrı sayaç var”", q5.SimpleDetail);
        Assert.Equal("Makinelere elektrik harcamasını gösteren sayaç taktırın", q5.SimpleTitle);

        var energy = c.RecommendationFollowUp.Items.Single(i =>
            i.TriggerType == RecommendationTrigger.Dimension && i.Dimension == Dimension.EnergyEfficiency);
        Assert.Equal("Enerji tasarrufu puanı: 0 → 50 (100 üzerinden)", energy.SimpleDetail);

        var q9 = c.QuestionChanges.Single(q => q.QuestionNumber == 9);
        Assert.Equal(("Hayır, hiç hesaplamadık", "Her yıl hesaplıyoruz"), (q9.PreviousLabel, q9.CurrentLabel));
        Assert.Equal("Enerji tasarrufu", c.DimensionChanges.Single(d => d.Dimension == Dimension.EnergyEfficiency).SimpleName);
    }

    [Fact]
    public async Task Gelisim_HedefVeKarbonDegisimiSadeMaddelerde()
    {
        var (db, company, _) = await ProgressScenarioAsync();
        await using var _db = db;
        await TestDb.AddFootprintAsync(db, company, 2023, 1000m);
        await TestDb.AddFootprintAsync(db, company, 2025, 900m);
        var service = new ProgressService(db, FixedTimeProvider.At(2026, 8, 1));
        await service.SetTargetAsync(company, new CarbonTargetRequest { BaseYear = 2023, TargetYear = 2030, ReductionPercent = 20m });

        var p = (await service.GetProgressAsync(company))!;

        Assert.Contains("Karbonunuz 2023 yılından bu yana yüzde 10 azaldı.", p.SimpleHighlights);
        Assert.Contains(p.Carbon.Target!.SimpleSummary, p.SimpleHighlights);
    }

    [Theory]
    [InlineData(179, false)]
    [InlineData(200, true)]
    public async Task Gelisim_YenilemeHatirlatmasiAyOlarak(int days, bool expected)
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        await TestDb.AddAssessmentAsync(db, company.Id, _ => 3, January);

        var p = (await new ProgressService(db, new FixedTimeProvider(new DateTimeOffset(January.AddDays(days)))).GetProgressAsync(company.Id))!;

        Assert.Equal(expected, p.SimpleHighlights.Contains("Son testinizin üzerinden yaklaşık 6 ay geçti. Testi yeniden doldurmanızı öneririz."));
    }

    // ===================== Jargon ve okunabilirlik taraması =====================

    /// <summary>Üretilebilen tüm sade rapor cümleleri (farklı senaryolardan) teknik terim içermemeli.</summary>
    [Fact]
    public async Task TumSadeRaporCumleleri_JargonIcermez()
    {
        var texts = new List<string?>();

        // Karbon: her kaynak en büyük kaynak olarak + karşılaştırma + Avrupa notu
        foreach (var source in Enum.GetValues<EmissionSource>())
        {
            texts.Add(PlainLanguage.CarbonGuidance(source));
            texts.Add(PlainLanguage.ScopeName(source == EmissionSource.Electricity ? EmissionScope.Scope2 : EmissionScope.Scope1));
        }
        texts.AddRange(new[] { PlainLanguage.CarbonLimitation, PlainLanguage.CarbonCbamNote,
            PlainLanguage.CarbonComparison(2024, -5m, -10m), PlainLanguage.CarbonComparison(2024, 5m, 10m) });

        // CBAM: tüm ürün grupları, ihracat var/yok, üç risk seviyesi
        foreach (var group in Enum.GetValues<CbamProductGroup>())
        foreach (var exports in new[] { true, false })
        foreach (var score in new[] { 10m, 50m, 90m })
        {
            var r = CbamRiskEvaluator.Evaluate(exports, group, score);
            texts.Add(r.SimpleRiskLevel);
            texts.Add(r.SimpleSummary);
            texts.AddRange(r.SimpleActions);
        }

        // Hedef: tüm durumlar
        foreach (var years in new[]
                 {
                     new[] { new Y(2023, 1m) }, new[] { new Y(2022, 1000m) },
                     new[] { new Y(2022, 1000m), new Y(2024, 950m) }, new[] { new Y(2022, 1000m), new Y(2024, 980m) },
                     new[] { new Y(2022, 1000m), new Y(2024, 1100m) }, new[] { new Y(2022, 1000m), new Y(2024, 1000m) },
                     new[] { new Y(2022, 1000m), new Y(2026, 700m) }
                 })
        {
            var t = CarbonTargetEvaluator.Evaluate(Target, years);
            texts.Add(t.SimpleStatusName);
            texts.Add(t.SimpleSummary);
        }

        // Gelişim
        var (db, _, p) = await ProgressScenarioAsync();
        await using (db)
        {
            texts.AddRange(p.SimpleHighlights);
            texts.Add(p.LatestComparison!.SimpleMaturityChange);
            texts.AddRange(p.LatestComparison.RecommendationFollowUp.Items.SelectMany(i => new[] { i.SimpleTitle, i.SimpleDetail }));
        }

        var problems = JargonIn(texts);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public async Task Okunabilirlik_SadeGelisimMaddeleri_TeknikMaddelerdenKolay()
    {
        var (db, _, p) = await ProgressScenarioAsync();
        await using var _db = db;

        var simple = Readability.Atesman(string.Join(" ", p.SimpleHighlights));
        var technical = Readability.Atesman(string.Join(" ", p.Highlights));

        Assert.True(simple.Score > technical.Score, $"Sade {simple.Score} / teknik {technical.Score}");
        Assert.True(simple.Score >= 50m, $"Sade gelişim maddeleri Ateşman {simple.Score} ({simple.Level})");
    }
}
