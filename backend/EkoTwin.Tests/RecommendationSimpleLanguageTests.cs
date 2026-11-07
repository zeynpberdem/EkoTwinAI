using System.Globalization;
using EkoTwin.Data;
using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Tests;

/// <summary>
/// Önerilerin sade dil katmanı: seed metinleri (jargon, uzunluk, okunabilirlik) ve
/// motorun ürettiği sade gerekçeler / snapshot alanları.
/// </summary>
public class RecommendationSimpleLanguageTests
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly Recommendation[] Templates = RecommendationSeed.Recommendations;

    private static int WordCount(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private static List<AssessmentRecommendation> Run(Func<int, int> score, bool cbamHigh = false, bool showCbam = false, bool withLabels = true)
    {
        var answers = QuestionSeed.Questions
            .Select(q =>
            {
                var s = score(q.DisplayOrder);
                var label = withLabels ? QuestionSeed.Options.Single(o => o.QuestionId == q.Id && o.Score == s).Label : null;
                return new RecommendationEngine.AnswerInput(q.Id, q.DisplayOrder, q.Dimension, s, label);
            })
            .ToList();
        var scores = answers.GroupBy(a => a.Dimension)
            .ToDictionary(g => g.Key, g => ScoreCalculator.DimensionScore(g.Select(a => a.Score)));
        return RecommendationEngine.Generate(Templates, answers, scores, cbamHigh, showCbam);
    }

    private static string[] Lines(string s) => s.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    // ===================== Seed metinleri =====================

    [Fact]
    public void HerOnerininSadeBasligiVeKazanimiVar_TeknikBasliktanFarkli()
    {
        foreach (var r in Templates)
        {
            Assert.False(string.IsNullOrWhiteSpace(r.SimpleTitle), $"{r.Title}: sade başlık eksik");
            Assert.False(string.IsNullOrWhiteSpace(r.SimpleBenefit), $"{r.Title}: kazanım metni eksik");
            Assert.NotEqual(r.Title, r.SimpleTitle);
        }
    }

    [Fact]
    public void SadeCbamNotu_YalnizcaTeknikCbamNotuOlanOnerilerde()
    {
        foreach (var r in Templates)
            Assert.Equal(r.CbamNote is not null, r.SimpleCbamNote is not null);
    }

    [Fact]
    public void SadeMetinlerde_Jargon_Yok()
    {
        var problems = new List<string>();
        foreach (var r in Templates)
        foreach (var text in new[] { r.SimpleTitle, r.SimpleBenefit, r.SimpleCbamNote }.OfType<string>())
        foreach (var term in QuestionSeedTests.Jargon)
        {
            if (Tr.CompareInfo.IndexOf(text, term, CompareOptions.IgnoreCase) >= 0)
                problems.Add($"\"{term}\" → {text}");
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void Uzunluk_BaslikEnFazla12_KazanimEnFazla25Kelime_VeSinirIcinde()
    {
        foreach (var r in Templates)
        {
            Assert.True(WordCount(r.SimpleTitle) <= 12, $"Başlık uzun: {r.SimpleTitle}");
            Assert.True(WordCount(r.SimpleBenefit) <= 25, $"Kazanım uzun: {r.SimpleBenefit}");
            Assert.True(r.SimpleTitle.Length <= 150 && r.SimpleBenefit.Length <= 300);
            Assert.True(r.SimpleCbamNote is null || r.SimpleCbamNote.Length <= 300);
        }
    }

    [Fact]
    public void Okunabilirlik_KazanimMetinleriBirlikte_EnAzOrta()
    {
        var all = Readability.Atesman(string.Join(" ", Templates.Select(r => r.SimpleBenefit)));
        Assert.True(all.Score >= 50m, $"Kazanım metinleri Ateşman {all.Score} ({all.Level})");
    }

    // ===================== Motorun sade gerekçeleri =====================

    [Fact]
    public void SoruBazliGerekce_SecilenCevabinMetniniKullanir()
    {
        var q9 = Run(n => n == 9 ? 1 : 4).Single(r => r.QuestionNumber == 9);

        Assert.Equal("Hayır, hiç hesaplamadık", q9.AnswerLabel);
        Assert.Equal("Cevabınız: \"Hayır, hiç hesaplamadık\"", Lines(q9.SimpleReasons)[0]);
        Assert.Equal("Soru 9 yanıtınız: 1/5", Lines(q9.Reasons)[0]); // teknik gerekçe korunur
    }

    [Fact]
    public void CevapMetniYoksa_YineDeAnlasilirGerekceVerir()
    {
        var q9 = Run(n => n == 9 ? 1 : 4, withLabels: false).Single(r => r.QuestionNumber == 9);

        Assert.Null(q9.AnswerLabel);
        Assert.StartsWith("Soru 9 cevabınız", Lines(q9.SimpleReasons)[0]);
    }

    [Fact]
    public void BoyutGerekcesi_SadeAdVeTamSayiPuan()
    {
        // Karbon soruları 1,1,1,2 → (1,25 − 1) / 4 × 100 = 6,25 → "6"
        var alert = Run(n => n switch { 9 or 10 or 11 => 1, 12 => 2, _ => 4 })
            .Single(r => r.TriggerType == RecommendationTrigger.Dimension && r.Dimension == Dimension.CarbonManagement);

        Assert.Equal("Karbon takibi puanınız 100 üzerinden 6", Lines(alert.SimpleReasons)[0]);
        Assert.Contains("En çok zorlandığınız konu: Karbon takibi", Lines(alert.SimpleReasons));
    }

    [Fact]
    public void CbamVeMaliyetVeEtki_SadeGerekceler()
    {
        var q9 = Run(n => n == 9 ? 1 : 4, cbamHigh: true, showCbam: true).Single(r => r.QuestionNumber == 9);
        var simple = Lines(q9.SimpleReasons);

        Assert.Contains("Avrupa'ya sattığınız ürünler karbon kurallarından etkileniyor", simple);
        Assert.Contains("Az masraflı", simple);
        Assert.Contains("Etkisi büyük", simple);
        Assert.Equal(Lines(q9.Reasons).Length, simple.Length); // her teknik gerekçenin sade karşılığı var
    }

    [Fact]
    public void SadeAlanlarSnapshotaYazilir_CbamNotuYalnizcaKapsamdakilere()
    {
        var shown = Run(n => n == 9 ? 1 : 4, showCbam: true).Single(r => r.QuestionNumber == 9);
        var hidden = Run(n => n == 9 ? 1 : 4, showCbam: false).Single(r => r.QuestionNumber == 9);

        Assert.Equal("Firmanızın yıllık karbonunu hesaplayın", shown.SimpleTitle);
        Assert.False(string.IsNullOrEmpty(shown.SimpleBenefit));
        Assert.NotNull(shown.SimpleCbamNote);
        Assert.Null(hidden.SimpleCbamNote);
        Assert.Null(hidden.CbamNote);
    }
}
