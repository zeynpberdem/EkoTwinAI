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
            .Select(d => new DimensionChangeResponse(d, ScoreCalculator.DisplayName(d), ScoreCalculator.SimpleName(d),
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
                return new QuestionChangeResponse(c.Question.DisplayOrder, c.Question.Text, c.Question.SimpleText,
                    c.Question.Dimension, p.Score, c.Score, c.Score - p.Score, Label(p), Label(c));
            })
            .ToList();

        // ---- Seviye ----
        var prevLevel = ScoreCalculator.MaturityLevel(previous.TotalScore);
        var currLevel = ScoreCalculator.MaturityLevel(current.TotalScore);
        var levelChanged = prevLevel != currLevel;
        var simpleChange = levelChanged
            ? $"{ScoreCalculator.SimpleMaturityLevel(previous.TotalScore)} → {ScoreCalculator.SimpleMaturityLevel(current.TotalScore)}"
            : null;

        return new AssessmentComparisonResponse(
            AssessmentSummaryResponse.From(previous),
            AssessmentSummaryResponse.From(current),
            (int)(current.CreatedAt - previous.CreatedAt).TotalDays,
            current.TotalScore - previous.TotalScore,
            levelChanged,
            levelChanged ? $"{prevLevel} → {currLevel}" : null,
            simpleChange,
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
            string simpleDetail;

            if (r.TriggerType == RecommendationTrigger.Question && r.QuestionNumber is { } n)
            {
                var before = r.AnswerScore ?? prevAnswers.GetValueOrDefault(n)?.Score;
                if (currAnswers.TryGetValue(n, out var now))
                {
                    improved = now.Score > RecommendationEngine.QuestionTriggerMaxScore;
                    detail = $"Soru {n}: {before}/5 → {now.Score}/5";
                    simpleDetail = AnswerChange(n, r.AnswerLabel ?? Label(prevAnswers.GetValueOrDefault(n)), Label(now), before, now.Score);
                }
                else
                {
                    improved = false;
                    detail = $"Soru {n} yeni değerlendirmede yer almıyor";
                    simpleDetail = "Bu soru artık sorulmuyor.";
                }
            }
            else
            {
                improved = currScores[r.Dimension] >= RecommendationEngine.DimensionTriggerThreshold;
                detail = $"{ScoreCalculator.DisplayName(r.Dimension)} skoru: " +
                         $"{Number(prevScores[r.Dimension])} → {Number(currScores[r.Dimension])}";
                simpleDetail = DimensionChange(r.Dimension, prevScores, currScores);
            }

            var status = improved ? FollowUpStatus.Improved : FollowUpStatus.StillOpen;
            items.Add(Item(r, status, detail, simpleDetail));
        }

        // Yeni çıkan öneriler
        var previousKeys = previousRecs.Select(Key).ToHashSet();
        foreach (var r in current.Recommendations.OrderBy(r => r.TriggerType).ThenBy(r => r.Rank))
        {
            if (previousKeys.Contains(Key(r)))
                continue;

            var isQuestion = r.TriggerType == RecommendationTrigger.Question && r.QuestionNumber is not null;
            var n = r.QuestionNumber ?? 0;
            var detail = isQuestion
                ? $"Soru {n}: {prevAnswers.GetValueOrDefault(n)?.Score.ToString() ?? "-"}/5 → {r.AnswerScore}/5"
                : $"{ScoreCalculator.DisplayName(r.Dimension)} skoru: " +
                  $"{Number(prevScores[r.Dimension])} → {Number(currScores[r.Dimension])}";
            var simpleDetail = isQuestion
                ? AnswerChange(n, Label(prevAnswers.GetValueOrDefault(n)), r.AnswerLabel ?? Label(currAnswers.GetValueOrDefault(n)),
                    prevAnswers.GetValueOrDefault(n)?.Score, r.AnswerScore)
                : DimensionChange(r.Dimension, prevScores, currScores);

            items.Add(Item(r, FollowUpStatus.New, detail, simpleDetail) with { WasTopPriority = false });
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
            SummaryText(previousItems.Count, improvedCount, top.Count, topImproved, newCount),
            SimpleSummaryText(previousItems.Count, improvedCount, top.Count, topImproved, newCount));

        return new RecommendationFollowUpResponse(summary, items);
    }

    private static FollowUpItemResponse Item(AssessmentRecommendation r, FollowUpStatus status, string detail, string simpleDetail) => new(
        r.Title, r.TriggerType, r.Dimension, ScoreCalculator.DisplayName(r.Dimension), r.QuestionNumber,
        r.IsTopPriority, status, StatusName(status), detail,
        string.IsNullOrEmpty(r.SimpleTitle) ? r.Title : r.SimpleTitle,
        simpleDetail);

    /// <summary>Seçilen cevabın metni (soru seçenekleri yüklüyse).</summary>
    private static string? Label(AssessmentDetail? d) =>
        d?.Question.Options.FirstOrDefault(o => o.Score == d.Score)?.Label;

    /// <summary>Sade cevap değişimi: Önce: "Hayır, hiç hesaplamadık" → Şimdi: "Her yıl hesaplıyoruz".</summary>
    private static string AnswerChange(int number, string? before, string? now, int? beforeScore, int? nowScore) =>
        before is not null && now is not null
            ? $"Önce: “{before}” → Şimdi: “{now}”"
            : $"Soru {number}: önce {beforeScore?.ToString() ?? "-"}, şimdi {nowScore?.ToString() ?? "-"} (5 üzerinden)";

    private static string DimensionChange(Dimension d, IReadOnlyDictionary<Dimension, decimal> prev, IReadOnlyDictionary<Dimension, decimal> curr) =>
        $"{ScoreCalculator.SimpleName(d)} puanı: {PlainLanguage.Points(prev[d])} → {PlainLanguage.Points(curr[d])} (100 üzerinden)";

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

    /// <summary>
    /// Öneri takibinin sade özeti. "Yaptınız" demek yerine "ilerleme var" denir: durum firmanın
    /// yeni cevaplarından çıkarılır, işin gerçekten yapıldığı ayrıca doğrulanmaz.
    /// </summary>
    private static string SimpleSummaryText(int previousCount, int improved, int topCount, int topImproved, int newCount)
    {
        if (previousCount == 0)
            return newCount > 0 ? $"Bu sefer {newCount} yeni konu çıktı." : "Şu an önerilecek yeni bir iş yok.";

        var text = improved > 0
            ? $"Geçen sefer önerilen {previousCount} işten {improved} tanesinde ilerleme var."
            : $"Geçen sefer önerilen {previousCount} işte henüz ilerleme görünmüyor.";

        if (topCount > 0)
            text += topImproved == topCount ? $" En acil {topCount} işin hepsinde ilerleme var."
                  : topImproved == 0 ? $" En acil {topCount} iş hâlâ bekliyor."
                  : $" En acil {topCount} işten {topImproved} tanesinde ilerleme var.";

        if (newCount > 0)
            text += $" {newCount} yeni konu çıktı.";

        return text;
    }

    /// <summary>Gelişim sayfasında KOBİ sahibine gösterilen sade özet maddeleri.</summary>
    public static IReadOnlyList<string> SimpleHighlights(
        int assessmentCount,
        AssessmentComparisonResponse? comparison,
        int? daysSinceLastAssessment,
        CarbonTrendResponse carbon)
    {
        var list = new List<string>();

        if (assessmentCount == 0)
            list.Add("Henüz test doldurmadınız. İlk testle başlayın.");
        else if (assessmentCount == 1)
            list.Add("Gelişiminizi görmek için testi yaklaşık 6 ay sonra yeniden doldurun.");

        if (comparison is not null)
        {
            var before = PlainLanguage.Points(comparison.Previous.TotalScore);
            var now = PlainLanguage.Points(comparison.Current.TotalScore);
            list.Add(comparison.TotalChange > 0 ? $"Genel puanınız yükseldi: {before} → {now} (100 üzerinden)."
                   : comparison.TotalChange < 0 ? $"Genel puanınız düştü: {before} → {now} (100 üzerinden)."
                   : $"Genel puanınız değişmedi: {now} (100 üzerinden).");

            if (comparison.SimpleMaturityChange is { } level)
                list.Add(comparison.TotalChange > 0 ? $"Seviyeniz yükseldi: {level}." : $"Seviyeniz düştü: {level}.");

            if (comparison.MostImproved is { } up)
                list.Add($"En çok ilerlediğiniz konu: {ScoreCalculator.SimpleName(up)}.");

            if (comparison.MostDeclined is { } down)
                list.Add($"Dikkat: {ScoreCalculator.SimpleName(down)} konusunda geriye gittiniz.");

            if (comparison.RecommendationFollowUp.Summary.PreviousCount > 0)
                list.Add(comparison.RecommendationFollowUp.Summary.SimpleSummary);
        }

        if (daysSinceLastAssessment is >= ReassessmentIntervalDays)
            list.Add($"Son testinizin üzerinden yaklaşık {daysSinceLastAssessment / 30} ay geçti. Testi yeniden doldurmanızı öneririz.");

        if (carbon.Series.Count == 0)
        {
            list.Add("Karbonunuzu hesaplayarak takibe başlayın.");
        }
        else
        {
            if (carbon.ChangeSinceBasePercent is { } change && change != 0)
                list.Add($"Karbonunuz {carbon.BaseYear} yılından bu yana {PlainLanguage.Percent(change)} {(change < 0 ? "azaldı" : "arttı")}.");

            list.Add(carbon.Target?.SimpleSummary
                     ?? "Karbonu azaltmak için bir hedef koyarsanız ilerlemenizi burada görürsünüz.");
        }

        return list;
    }
}
