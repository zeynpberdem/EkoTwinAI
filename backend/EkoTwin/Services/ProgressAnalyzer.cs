using EkoTwin.Dtos;
using EkoTwin.Models;
using static EkoTwin.Services.TurkishFormat;

namespace EkoTwin.Services;

/// <summary>
/// Gelişim takibi: iki değerlendirmeyi karşılaştırır ve önceki önerilerin akıbetini çıkarır.
/// Veritabanından bağımsızdır; değerlendirmelerin yanıtları (soru bilgisiyle) ve önerileri yüklenmiş olmalıdır.
///
/// ÖNERİ TAKİBİ (otomatik; firmadan ek bilgi istenmez)
///   Soru bazlı öneri  → yeni yanıt ≥ 3 ise "İyileşti", hâlâ 1-2 ise "Hâlâ açık"
///   Boyut bazlı uyarı → yeni boyut skoru ≥ 50 ise "İyileşti", aksi "Hâlâ açık"
///   Önceki raporda olmayan yeni öneriler → "Yeni" (bir alan gerilemiş demektir)
/// Bu eşikler öneri motorunun tetikleme eşikleriyle aynıdır (tek kaynak: RecommendationEngine).
/// </summary>
public static class ProgressAnalyzer
{
    /// <summary>Önerilen yeniden değerlendirme aralığı (gün).</summary>
    public const int ReassessmentIntervalDays = 180;

    public static string StatusName(FollowUpStatus status) => status switch
    {
        FollowUpStatus.Improved => "İyileşti",
        FollowUpStatus.StillOpen => "Hâlâ açık",
        FollowUpStatus.New => "Yeni",
        _ => status.ToString()
    };

    public static IReadOnlyDictionary<Dimension, decimal> ScoresOf(Assessment a) => new Dictionary<Dimension, decimal>
    {
        [Dimension.DigitalInfrastructure] = a.DigitalInfrastructureScore,
        [Dimension.EnergyEfficiency] = a.EnergyEfficiencyScore,
        [Dimension.CarbonManagement] = a.CarbonManagementScore,
        [Dimension.SupplyChain] = a.SupplyChainScore
    };

    /// <summary>İki değerlendirmeyi karşılaştırır. Sıra fark etmez; eski olan "önceki" sayılır.</summary>
    public static AssessmentComparisonResponse Compare(Assessment first, Assessment second)
    {
        var (previous, current) = first.CreatedAt <= second.CreatedAt ? (first, second) : (second, first);

        var prevScores = ScoresOf(previous);
        var currScores = ScoresOf(current);

        // ---- Boyutlar ----
        var dimensionChanges = Enum.GetValues<Dimension>()
            .Select(d => new DimensionChangeResponse(d, ScoreCalculator.DisplayName(d),
                prevScores[d], currScores[d], currScores[d] - prevScores[d]))
            .ToList();

        var mostImproved = dimensionChanges.Where(c => c.Change > 0)
            .OrderByDescending(c => c.Change).ThenBy(c => c.Dimension).FirstOrDefault();
        var mostDeclined = dimensionChanges.Where(c => c.Change < 0)
            .OrderBy(c => c.Change).ThenBy(c => c.Dimension).FirstOrDefault();

        // ---- Sorular (soru numarasıyla eşleşir) ----
        var prevAnswers = AnswersByNumber(previous);
        var currAnswers = AnswersByNumber(current);

        var questionChanges = currAnswers.Values
            .Where(c => prevAnswers.TryGetValue(c.Question.DisplayOrder, out var p) && p.Score != c.Score)
            .OrderBy(c => c.Question.DisplayOrder)
            .Select(c =>
            {
                var p = prevAnswers[c.Question.DisplayOrder];
                return new QuestionChangeResponse(c.Question.DisplayOrder, c.Question.Text, c.Question.Dimension,
                    p.Score, c.Score, c.Score - p.Score);
            })
            .ToList();

        // ---- Seviye ----
        var prevLevel = ScoreCalculator.MaturityLevel(previous.TotalScore);
        var currLevel = ScoreCalculator.MaturityLevel(current.TotalScore);
        var levelChanged = prevLevel != currLevel;

        return new AssessmentComparisonResponse(
            AssessmentSummaryResponse.From(previous),
            AssessmentSummaryResponse.From(current),
            (int)(current.CreatedAt - previous.CreatedAt).TotalDays,
            current.TotalScore - previous.TotalScore,
            levelChanged,
            levelChanged ? $"{prevLevel} → {currLevel}" : null,
            dimensionChanges,
            mostImproved?.Dimension, mostImproved?.DimensionName,
            mostDeclined?.Dimension, mostDeclined?.DimensionName,
            questionChanges,
            FollowUp(previous, current, prevScores, currScores, prevAnswers, currAnswers));
    }

    /// <summary>Gelişim sayfasındaki sade dilde özet maddeleri.</summary>
    public static IReadOnlyList<string> Highlights(
        int assessmentCount,
        AssessmentComparisonResponse? comparison,
        int? daysSinceLastAssessment,
        CarbonTrendResponse carbon)
    {
        var list = new List<string>();

        if (assessmentCount == 0)
            list.Add("Henüz değerlendirme yapılmamış; ilk değerlendirmeyle başlayın.");
        else if (assessmentCount == 1)
            list.Add("Tek değerlendirmeniz var; gelişimi görmek için yaklaşık 6 ay sonra yeniden değerlendirme yapın.");

        if (comparison is not null)
        {
            list.Add($"Genel skorunuz {Number(comparison.Previous.TotalScore)} → {Number(comparison.Current.TotalScore)} " +
                     $"({Signed(comparison.TotalChange)}).");

            if (comparison.MaturityLevelChanged)
                list.Add(comparison.TotalChange > 0
                    ? $"Olgunluk seviyeniz yükseldi: {comparison.MaturityChange}."
                    : $"Olgunluk seviyeniz düştü: {comparison.MaturityChange}.");

            if (comparison.MostImproved is { } up)
            {
                var c = comparison.DimensionChanges.Single(d => d.Dimension == up);
                list.Add($"En çok gelişen boyut: {c.DimensionName} ({Signed(c.Change)}).");
            }

            if (comparison.MostDeclined is { } down)
            {
                var c = comparison.DimensionChanges.Single(d => d.Dimension == down);
                list.Add($"Dikkat: {c.DimensionName} {Number(Math.Abs(c.Change))} puan geriledi.");
            }

            if (comparison.RecommendationFollowUp.Summary.PreviousCount > 0)
                list.Add(comparison.RecommendationFollowUp.Summary.Summary);
        }

        if (daysSinceLastAssessment is >= ReassessmentIntervalDays)
            list.Add($"Son değerlendirmeniz {daysSinceLastAssessment} gün önce yapıldı; yenilemeniz önerilir.");

        if (carbon.Series.Count == 0)
        {
            list.Add("Karbon ayak izinizi hesaplayarak emisyon takibine başlayın.");
        }
        else
        {
            if (carbon.ChangeSinceBasePercent is { } change && change != 0)
                list.Add(change < 0
                    ? $"Karbon emisyonunuz {carbon.BaseYear} yılından bu yana %{Number(Math.Abs(change), "0.#")} azaldı."
                    : $"Karbon emisyonunuz {carbon.BaseYear} yılından bu yana %{Number(change, "0.#")} arttı.");

            list.Add(carbon.Target?.Summary
                     ?? "Bir karbon azaltım hedefi belirleyerek ilerlemenizi izleyebilirsiniz.");
        }

        return list;
    }

    // ------------------------------------------------------------------

    private static Dictionary<int, AssessmentDetail> AnswersByNumber(Assessment a) =>
        a.AssessmentDetails.ToDictionary(d => d.Question.DisplayOrder);

    private static RecommendationFollowUpResponse FollowUp(
        Assessment previous, Assessment current,
        IReadOnlyDictionary<Dimension, decimal> prevScores, IReadOnlyDictionary<Dimension, decimal> currScores,
        IReadOnlyDictionary<int, AssessmentDetail> prevAnswers, IReadOnlyDictionary<int, AssessmentDetail> currAnswers)
    {
        var items = new List<FollowUpItemResponse>();

        // Önceki öneriler: önce aksiyonlar (sırasıyla), sonra stratejik uyarılar
        var previousRecs = previous.Recommendations
            .OrderBy(r => r.TriggerType)
            .ThenBy(r => r.Rank)
            .ToList();

        foreach (var r in previousRecs)
        {
            bool improved;
            string detail;

            if (r.TriggerType == RecommendationTrigger.Question && r.QuestionNumber is { } n)
            {
                var before = r.AnswerScore ?? prevAnswers.GetValueOrDefault(n)?.Score;
                if (currAnswers.TryGetValue(n, out var now))
                {
                    improved = now.Score > RecommendationEngine.QuestionTriggerMaxScore;
                    detail = $"Soru {n}: {before}/5 → {now.Score}/5";
                }
                else
                {
                    improved = false;
                    detail = $"Soru {n} yeni değerlendirmede yer almıyor";
                }
            }
            else
            {
                improved = currScores[r.Dimension] >= RecommendationEngine.DimensionTriggerThreshold;
                detail = $"{ScoreCalculator.DisplayName(r.Dimension)} skoru: " +
                         $"{Number(prevScores[r.Dimension])} → {Number(currScores[r.Dimension])}";
            }

            var status = improved ? FollowUpStatus.Improved : FollowUpStatus.StillOpen;
            items.Add(Item(r, status, detail));
        }

        // Yeni çıkan öneriler
        var previousKeys = previousRecs.Select(Key).ToHashSet();
        foreach (var r in current.Recommendations.OrderBy(r => r.TriggerType).ThenBy(r => r.Rank))
        {
            if (previousKeys.Contains(Key(r)))
                continue;

            var detail = r.TriggerType == RecommendationTrigger.Question && r.QuestionNumber is { } n
                ? $"Soru {n}: {prevAnswers.GetValueOrDefault(n)?.Score.ToString() ?? "-"}/5 → {r.AnswerScore}/5"
                : $"{ScoreCalculator.DisplayName(r.Dimension)} skoru: " +
                  $"{Number(prevScores[r.Dimension])} → {Number(currScores[r.Dimension])}";

            items.Add(Item(r, FollowUpStatus.New, detail) with { WasTopPriority = false });
        }

        var previousItems = items.Where(i => i.Status != FollowUpStatus.New).ToList();
        var improvedCount = previousItems.Count(i => i.Status == FollowUpStatus.Improved);
        var top = previousItems.Where(i => i.WasTopPriority).ToList();
        var topImproved = top.Count(i => i.Status == FollowUpStatus.Improved);
        var newCount = items.Count - previousItems.Count;

        var summary = new FollowUpSummaryResponse(
            previousItems.Count,
            improvedCount,
            previousItems.Count - improvedCount,
            newCount,
            top.Count,
            topImproved,
            SummaryText(previousItems.Count, improvedCount, top.Count, topImproved, newCount));

        return new RecommendationFollowUpResponse(summary, items);
    }

    private static FollowUpItemResponse Item(AssessmentRecommendation r, FollowUpStatus status, string detail) => new(
        r.Title, r.TriggerType, r.Dimension, ScoreCalculator.DisplayName(r.Dimension), r.QuestionNumber,
        r.IsTopPriority, status, StatusName(status), detail);

    /// <summary>Öneriyi iki rapor arasında eşleştirme anahtarı: şablon Id'si (şablon silinmişse başlık).</summary>
    private static string Key(AssessmentRecommendation r) => r.RecommendationId?.ToString() ?? r.Title;

    private static string SummaryText(int previousCount, int improved, int topCount, int topImproved, int newCount)
    {
        if (previousCount == 0)
            return newCount > 0
                ? $"Önceki değerlendirmede öneri yoktu; bu sefer {newCount} yeni öneri çıktı."
                : "Önceki değerlendirmede de bu değerlendirmede de öneri yok.";

        var text = $"Önceki {previousCount} öneriden {improved} tanesinde iyileşme var";
        if (topCount > 0)
            text += $"; en acil {topCount} adımdan {topImproved} tanesi iyileşti";
        if (newCount > 0)
            text += $"; {newCount} yeni öneri çıktı";
        return text + ".";
    }
}
