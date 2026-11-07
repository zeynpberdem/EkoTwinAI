using EkoTwin.Data;
using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Tests;

/// <summary>
/// Öneri motoru testleri: tetikleme kuralları, aciliyet puanı, sıralama, "en acil 3 adım",
/// darboğaz ve CBAM etkisi. Gerçek öneri kütüphanesi (RecommendationSeed) kullanılır.
/// </summary>
public class RecommendationEngineTests
{
    private static readonly Recommendation[] Templates = RecommendationSeed.Recommendations;

    /// <summary>16 sorunun yanıtlarını soru numarasına göre üretir.</summary>
    private static List<RecommendationEngine.AnswerInput> Answers(Func<int, int> scoreByNumber) =>
        QuestionSeed.Questions
            .Select(q => new RecommendationEngine.AnswerInput(q.Id, q.DisplayOrder, q.Dimension, scoreByNumber(q.DisplayOrder)))
            .ToList();

    /// <summary>Yanıtlardan boyut skorlarını gerçek formülle hesaplar.</summary>
    private static Dictionary<Dimension, decimal> Scores(List<RecommendationEngine.AnswerInput> answers) =>
        answers.GroupBy(a => a.Dimension)
               .ToDictionary(g => g.Key, g => ScoreCalculator.DimensionScore(g.Select(a => a.Score)));

    private static List<AssessmentRecommendation> Run(Func<int, int> scoreByNumber, bool cbamHigh = false, bool showCbam = false)
    {
        var answers = Answers(scoreByNumber);
        return RecommendationEngine.Generate(Templates, answers, Scores(answers), cbamHigh, showCbam);
    }

    private static List<AssessmentRecommendation> Actions(List<AssessmentRecommendation> r) =>
        r.Where(x => x.TriggerType == RecommendationTrigger.Question).OrderBy(x => x.Rank).ToList();

    private static List<AssessmentRecommendation> Strategic(List<AssessmentRecommendation> r) =>
        r.Where(x => x.TriggerType == RecommendationTrigger.Dimension).OrderBy(x => x.Rank).ToList();

    // ===================== Tetikleme =====================

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(5, false)]
    public void SoruBazliOneri_YalnizcaBirVeyaIkiPuandaTetiklenir(int scoreForQ5, bool expected)
    {
        var result = Run(n => n == 5 ? scoreForQ5 : 4);

        Assert.Equal(expected, Actions(result).Any(r => r.QuestionNumber == 5));
    }

    [Fact]
    public void HerSoruIcinBirOneriVardir_HepsiDusukseOnAltiAksiyonUretilir()
    {
        var result = Run(_ => 1);

        Assert.Equal(16, Actions(result).Count);
        Assert.Equal(Enumerable.Range(1, 16), Actions(result).Select(a => a.QuestionNumber!.Value).Order());
    }

    [Fact]
    public void BoyutBazliUyari_SkorEllininAltindaysaTetiklenir_EsitseTetiklenmez()
    {
        // Karbon (9-12): hepsi 3 → skor 50 (eşik dahil değil) | Enerji (5-8): hepsi 2 → 25
        var result = Run(n => n is >= 9 and <= 12 ? 3 : n is >= 5 and <= 8 ? 2 : 5);

        var strategic = Strategic(result);
        Assert.Single(strategic);
        Assert.Equal(Dimension.EnergyEfficiency, strategic[0].Dimension);
    }

    [Fact]
    public void HerSeyIyiyse_HicOneriUretilmez()
    {
        var result = Run(_ => 4);

        Assert.Empty(result);
    }

    [Fact]
    public void PasifSablonTetiklenmez()
    {
        var templates = Templates.Select(t => new Recommendation
        {
            Id = t.Id, TriggerType = t.TriggerType, QuestionId = t.QuestionId, Dimension = t.Dimension,
            Title = t.Title, Rationale = t.Rationale, Priority = t.Priority, CostLevel = t.CostLevel,
            ImpactLevel = t.ImpactLevel, ImpactType = t.ImpactType, DurationMinMonths = t.DurationMinMonths,
            DurationMaxMonths = t.DurationMaxMonths, IsActive = t.QuestionId != QuestionSeed.Questions[4].Id // Soru 5 pasif
        }).ToList();
        var answers = Answers(_ => 1);

        var result = RecommendationEngine.Generate(templates, answers, Scores(answers), false, false);

        Assert.DoesNotContain(result, r => r.QuestionNumber == 5);
    }

    // ===================== Sıralama ve "En acil 3 adım" =====================

    [Fact]
    public void EnAcilUcAdim_YalnizcaAksiyonlardanSecilir_StratejikUyarilarDahilEdilmez()
    {
        var result = Run(_ => 1);

        Assert.Equal(3, result.Count(r => r.IsTopPriority));
        Assert.All(result.Where(r => r.IsTopPriority), r => Assert.Equal(RecommendationTrigger.Question, r.TriggerType));
        Assert.All(Strategic(result), r => Assert.False(r.IsTopPriority));
    }

    [Fact]
    public void UctenAzAksiyonVarsa_HepsiEnAcilListesindedir()
    {
        var result = Run(n => n is 3 or 9 ? 1 : 4);

        Assert.Equal(2, result.Count(r => r.IsTopPriority));
    }

    [Fact]
    public void Siralama_AciliyetPuaninaGoreAzalandir_SiraNumaralari1denBaslar()
    {
        var actions = Actions(Run(_ => 1));

        Assert.Equal(Enumerable.Range(1, actions.Count), actions.Select(a => a.Rank));
        Assert.True(actions.Zip(actions.Skip(1)).All(p => p.First.UrgencyScore >= p.Second.UrgencyScore));
    }

    [Fact]
    public void AciliyetPuani_FormulleUyumludur()
    {
        // Soru 5: Etki Yüksek(6) + Maliyet Düşük(3) + Öncelik Yüksek(2) + yanıt 1(2) = 13
        // Enerji tek darboğaz (diğerleri 5) → +2 = 15
        var result = Run(n => n == 5 ? 1 : n is >= 6 and <= 8 ? 4 : 5);
        var q5 = Actions(result).Single(a => a.QuestionNumber == 5);

        Assert.Equal(15, q5.UrgencyScore);
    }

    [Fact]
    public void DarbogazBoyutu_AyniOnerininPuaniniIkiArtirir()
    {
        // Senaryo A: Enerji darboğaz (diğer boyutlar 5)
        var a = Run(n => n == 5 ? 1 : n is >= 6 and <= 8 ? 3 : 5);
        // Senaryo B: aynı Soru 5 yanıtı, ama Dijital daha zayıf → Enerji darboğaz değil
        var b = Run(n => n == 5 ? 1 : n is >= 6 and <= 8 ? 3 : n <= 4 ? 1 : 5);

        var inBottleneck = Actions(a).Single(x => x.QuestionNumber == 5);
        var notBottleneck = Actions(b).Single(x => x.QuestionNumber == 5);

        Assert.Equal(RecommendationEngine.BottleneckBonus, inBottleneck.UrgencyScore - notBottleneck.UrgencyScore);
        Assert.Contains("En zayıf boyutunuz: Enerji Verimliliği", inBottleneck.Reasons);
    }

    [Fact]
    public void CbamRiskiYuksekse_KarbonOnerisiEnerjiDarbogaziniGecerekBirinciOlur()
    {
        // Enerji en zayıf boyut (Soru 5→3, Soru 6-8→1 → skor 12,5), Karbon daha iyi (Soru 9→2, 10-12→3 → 43,75).
        // CBAM yokken: Soru 7 (13 puan) birinci, Soru 9 (12 puan) geride.
        // CBAM riski yüksekken: Soru 9 +3 → 15 puanla birinci sıraya çıkmalı.
        // (Soru 5 de CBAM ilişkili olduğu için bu senaryoda tetiklenmeyecek şekilde 3 verildi.)
        Func<int, int> scores = n => n switch
        {
            5 => 3,
            >= 6 and <= 8 => 1,
            9 => 2,
            >= 10 and <= 12 => 3,
            _ => 4
        };

        var withoutCbam = Actions(Run(scores, cbamHigh: false));
        var withCbam = Actions(Run(scores, cbamHigh: true, showCbam: true));

        Assert.Equal(7, withoutCbam[0].QuestionNumber);     // CBAM yokken enerji önerisi önde
        Assert.Equal(9, withCbam[0].QuestionNumber);        // CBAM riski yüksekken karbon hesabı birinci
        Assert.Equal(Level.High, withCbam[0].Priority);
        Assert.Contains("CBAM riskiniz yüksek", withCbam[0].Reasons);
    }

    // ===================== CBAM notu =====================

    [Fact]
    public void CbamNotu_YalnizcaKapsamdakiIhracatciyaGosterilir()
    {
        var shown = Actions(Run(_ => 1, showCbam: true)).Single(a => a.QuestionNumber == 9);
        var hidden = Actions(Run(_ => 1, showCbam: false)).Single(a => a.QuestionNumber == 9);

        Assert.False(string.IsNullOrEmpty(shown.CbamNote));
        Assert.Null(hidden.CbamNote);
    }

    // ===================== Gerekçeler =====================

    [Fact]
    public void Gerekceler_SoruYanitiniVeMaliyetEtkiBilgisiniIcerir()
    {
        var q3 = Actions(Run(n => n == 3 ? 1 : 4)).Single(a => a.QuestionNumber == 3);
        var reasons = q3.Reasons.Split('\n');

        Assert.Equal("Soru 3 yanıtınız: 1/5", reasons[0]);
        Assert.Contains("Düşük maliyet", reasons);
        Assert.Contains("Yüksek etki", reasons);
    }

    [Fact]
    public void Darbogaz_EsitlikteHepsiniDondurur()
    {
        var bottlenecks = RecommendationEngine.FindBottlenecks(new Dictionary<Dimension, decimal>
        {
            [Dimension.DigitalInfrastructure] = 25m,
            [Dimension.EnergyEfficiency] = 25m,
            [Dimension.CarbonManagement] = 50m,
            [Dimension.SupplyChain] = 75m
        });

        Assert.Equal(new HashSet<Dimension> { Dimension.DigitalInfrastructure, Dimension.EnergyEfficiency }, bottlenecks);
    }
}
