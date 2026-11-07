using EkoTwin.Data;
using EkoTwin.Models;

namespace EkoTwin.Tests;

/// <summary>
/// Öneri kütüphanesinin bütünlük testleri: metinler değiştirildiğinde kuralların bozulmadığını garanti eder.
/// </summary>
public class RecommendationSeedTests
{
    private static readonly Recommendation[] All = RecommendationSeed.Recommendations;

    [Fact]
    public void Toplam20Oneri_16SoruBazli_4BoyutBazli()
    {
        Assert.Equal(20, All.Length);
        Assert.Equal(16, All.Count(r => r.TriggerType == RecommendationTrigger.Question));
        Assert.Equal(4, All.Count(r => r.TriggerType == RecommendationTrigger.Dimension));
    }

    [Fact]
    public void HerSorununTamOlarakBirOnerisiVar()
    {
        var questionIds = QuestionSeed.Questions.Select(q => q.Id).OrderBy(x => x);
        var recQuestionIds = All.Where(r => r.QuestionId is not null).Select(r => r.QuestionId!.Value).OrderBy(x => x);

        Assert.Equal(questionIds, recQuestionIds);
    }

    [Fact]
    public void HerBoyutunTamOlarakBirStratejikUyarisiVar()
    {
        var dims = All.Where(r => r.Dimension is not null).Select(r => r.Dimension!.Value).OrderBy(d => d);

        Assert.Equal(Enum.GetValues<Dimension>().OrderBy(d => d), dims);
    }

    [Fact]
    public void TetiklemeAlanlariTutarli()
    {
        Assert.All(All, r =>
        {
            if (r.TriggerType == RecommendationTrigger.Question)
            {
                Assert.NotNull(r.QuestionId);
                Assert.Null(r.Dimension);
            }
            else
            {
                Assert.NotNull(r.Dimension);
                Assert.Null(r.QuestionId);
            }
        });
    }

    [Fact]
    public void IdlerBenzersiz_SurelerGecerli_MetinlerSinirIcinde()
    {
        Assert.Equal(All.Length, All.Select(r => r.Id).Distinct().Count());

        Assert.All(All, r =>
        {
            Assert.InRange(r.DurationMinMonths, 0, r.DurationMaxMonths);
            Assert.False(string.IsNullOrWhiteSpace(r.Title));
            Assert.InRange(r.Title.Length, 1, 200);
            Assert.InRange(r.Rationale.Length, 1, 1000);
            Assert.True((r.CbamNote?.Length ?? 0) <= 500);
            Assert.Equal(r.CbamNote is not null, r.CbamRelevant);
        });
    }

    [Fact]
    public void CbamIfadeleriYalnizcaCbamNotundaGecer()
    {
        // "CBAM" kelimesi genel metinde geçerse kapsam dışı firmalara da gösterilir.
        // İstisna: Soru 12'nin kendisi CBAM uyumunu sorar.
        var q12 = QuestionSeed.Questions.Single(q => q.DisplayOrder == 12).Id;

        Assert.All(All.Where(r => r.QuestionId != q12), r =>
        {
            Assert.DoesNotContain("CBAM", r.Title);
            Assert.DoesNotContain("CBAM", r.Rationale);
        });
    }

    [Fact]
    public void KaynaksizYuzdeKullanilmaz()
    {
        Assert.All(All.Where(r => r.Source is null), r =>
        {
            Assert.DoesNotContain("%", r.Title);
            Assert.DoesNotContain("%", r.Rationale);
        });
    }
}
