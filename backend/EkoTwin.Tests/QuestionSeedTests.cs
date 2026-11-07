using System.Globalization;
using EkoTwin.Data;
using EkoTwin.Services;

namespace EkoTwin.Tests;

/// <summary>
/// Soruların sade dil kuralları: yapı, jargon yasağı, uzunluk sınırları ve okunabilirlik.
/// Biri ileride ağır/teknik bir metin eklerse bu testler uyarır.
/// </summary>
public class QuestionSeedTests
{
    /// <summary>
    /// KOBİ sahibine gösterilen metinlerde (sade soru + seçenekler) geçmemesi gereken teknik kelimeler.
    /// Bu terimler yalnızca "Bu ne demek?" açıklamasında (HelpText), parantez içinde kullanılabilir.
    /// </summary>
    public static readonly string[] Jargon =
    {
        "ERP", "CRM", "LCA", "CBAM", "SKDM", "IoT", "Kapsam", "emisyon", "firewall", "lojistik",
        "optimiz", "entegre", "sürdürülebilir", "yetkinlik", "etüt", "otomasyon", "operasyon"
    };

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static int WordCount(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private static IEnumerable<string> OptionsOf(Guid questionId) =>
        QuestionSeed.Options.Where(o => o.QuestionId == questionId).OrderBy(o => o.Score).Select(o => o.Label);

    // ===================== Yapı =====================

    [Fact]
    public void Her4BoyuttaDortSoru_Toplam16()
    {
        Assert.Equal(16, QuestionSeed.Questions.Length);
        Assert.All(QuestionSeed.Questions.GroupBy(q => q.Dimension), g => Assert.Equal(4, g.Count()));
    }

    [Fact]
    public void HerSorununPuan1den5eTamBesSecenegiVar()
    {
        foreach (var q in QuestionSeed.Questions)
        {
            var scores = QuestionSeed.Options.Where(o => o.QuestionId == q.Id).Select(o => o.Score).OrderBy(s => s);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, scores);
        }
    }

    [Fact]
    public void SecenekMetinleri_DoluBenzersizVeSinirIcinde()
    {
        foreach (var q in QuestionSeed.Questions)
        {
            var labels = OptionsOf(q.Id).ToList();
            Assert.All(labels, l => Assert.False(string.IsNullOrWhiteSpace(l)));
            Assert.All(labels, l => Assert.True(l.Length <= 150, $"Soru {q.DisplayOrder}: seçenek 150 karakteri aşıyor"));
            Assert.Equal(labels.Count, labels.Distinct().Count());
        }
    }

    [Fact]
    public void SadeMetin_ResmiMetindenFarkliVeSinirIcinde()
    {
        foreach (var q in QuestionSeed.Questions)
        {
            Assert.NotEqual(q.Text, q.SimpleText);
            Assert.True(q.SimpleText.Length <= 300, $"Soru {q.DisplayOrder}: sade metin 300 karakteri aşıyor");
            Assert.True(q.HelpText is null || q.HelpText.Length <= 500, $"Soru {q.DisplayOrder}: açıklama 500 karakteri aşıyor");
            Assert.EndsWith("?", q.SimpleText);
        }
    }

    [Fact]
    public void SeedKimlikleri_Benzersiz()
    {
        var ids = QuestionSeed.Options.Select(o => o.Id).ToList();
        Assert.Equal(80, ids.Distinct().Count());
    }

    // ===================== Sade dil =====================

    [Fact]
    public void SadeMetinVeSeceneklerde_Jargon_Yok()
    {
        var problems = new List<string>();
        foreach (var q in QuestionSeed.Questions)
        {
            foreach (var text in OptionsOf(q.Id).Prepend(q.SimpleText))
            foreach (var term in Jargon)
            {
                if (Tr.CompareInfo.IndexOf(text, term, CompareOptions.IgnoreCase) >= 0)
                    problems.Add($"Soru {q.DisplayOrder}: \"{term}\" → {text}");
            }
        }

        Assert.True(problems.Count == 0, "Teknik terim yalnızca HelpText'te olmalı:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void SadeSorular_EnFazla15Kelime_Secenekler_EnFazla10Kelime()
    {
        foreach (var q in QuestionSeed.Questions)
        {
            Assert.True(WordCount(q.SimpleText) <= 15, $"Soru {q.DisplayOrder} çok uzun: {WordCount(q.SimpleText)} kelime");
            foreach (var label in OptionsOf(q.Id))
                Assert.True(WordCount(label) <= 10, $"Soru {q.DisplayOrder} seçeneği çok uzun: {label}");
        }
    }

    /// <summary>
    /// Ateşman formülü tek cümlede güvenilir değildir (paragraflar için tasarlanmıştır);
    /// bu yüzden metin bütünlerine uygulanır: tüm seçenekler "kolay", tüm açıklamalar en az "orta güçlükte".
    /// </summary>
    [Fact]
    public void Okunabilirlik_SeceneklerKolay_AciklamalarEnAzOrta()
    {
        var options = Readability.Atesman(string.Join(" ", QuestionSeed.Options.Select(o => o.Label + ".")));
        var help = Readability.Atesman(string.Join(" ", QuestionSeed.Questions.Where(q => q.HelpText != null).Select(q => q.HelpText)));

        Assert.True(options.Score >= 70m, $"Seçenekler Ateşman {options.Score} ({options.Level})");
        Assert.True(help.Score >= 50m, $"Açıklamalar Ateşman {help.Score} ({help.Level})");
    }
}
