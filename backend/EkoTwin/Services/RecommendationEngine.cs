using EkoTwin.Dtos;
using EkoTwin.Models;

namespace EkoTwin.Services;

/// <summary>
/// Öneri motoru: değerlendirme sonuçlarından firmaya özel, sıralı öneriler üretir.
/// Veritabanından bağımsızdır (saf hesaplama); kolayca test edilir.
///
/// 1) TETİKLEME (hibrit)
///    - Soru bazlı: soruya 1 veya 2 puan verildiyse  → somut aksiyon
///    - Boyut bazlı: boyut skoru 50'nin altındaysa    → stratejik uyarı
///
/// 2) ACİLİYET PUANI (şeffaf; raporda gerekçeleriyle gösterilir)
///    Etki düzeyi        Düşük/Orta/Yüksek  → +2 / +4 / +6
///    Maliyet            Yüksek/Orta/Düşük  → +1 / +2 / +3
///    Öncelik            Düşük/Orta/Yüksek  → +0 / +1 / +2
///    Ciddiyet           Soru: 1 puan → +2, 2 puan → +1 | Boyut: skor &lt; 25 → +2, aksi +1
///    Darboğaz boyutu    En düşük skorlu boyuttaysa → +2
///    CBAM               CBAM riski Yüksek ve öneri CBAM ilişkiliyse → +3 (öncelik Yüksek'e çıkar)
///
/// 3) SIRALAMA
///    Aksiyonlar ve stratejik uyarılar ayrı sıralanır. "En acil 3 adım" = en yüksek puanlı 3 AKSİYON
///    (somut ve uygulanabilir olduğu için). Eşitlikte: düşük yanıt/skor, düşük maliyet, soru sırası.
/// </summary>
public static class RecommendationEngine
{
    public const int QuestionTriggerMaxScore = 2;
    public const decimal DimensionTriggerThreshold = 50m;
    public const decimal SevereDimensionThreshold = 25m;
    public const int TopActionCount = 3;

    public const int BottleneckBonus = 2;
    public const int CbamBonus = 3;

    /// <summary>Bir sorunun yanıtı (motorun girdisi).</summary>
    public record AnswerInput(Guid QuestionId, int QuestionNumber, Dimension Dimension, int Score, string? AnswerLabel = null);

    /// <summary>
    /// Önerileri üretir ve sıralar. Dönen kayıtlar değerlendirmeye bağlanıp kaydedilir.
    /// </summary>
    /// <param name="templates">Aktif öneri şablonları.</param>
    /// <param name="answers">Firmanın yanıtları.</param>
    /// <param name="dimensionScores">Boyut skorları (0-100).</param>
    /// <param name="cbamRiskHigh">CBAM riski "Yüksek" mi?</param>
    /// <param name="showCbamNotes">Firma AB'ye CBAM kapsamında ürün ihraç ediyor mu? (CBAM notları gösterilir)</param>
    public static List<AssessmentRecommendation> Generate(
        IEnumerable<Recommendation> templates,
        IReadOnlyCollection<AnswerInput> answers,
        IReadOnlyDictionary<Dimension, decimal> dimensionScores,
        bool cbamRiskHigh,
        bool showCbamNotes)
    {
        var answersByQuestion = answers.ToDictionary(a => a.QuestionId);
        var bottlenecks = FindBottlenecks(dimensionScores);

        var actions = new List<Candidate>();
        var strategic = new List<Candidate>();

        foreach (var t in templates.Where(t => t.IsActive))
        {
            if (t.TriggerType == RecommendationTrigger.Question
                && t.QuestionId is { } qid
                && answersByQuestion.TryGetValue(qid, out var answer)
                && answer.Score <= QuestionTriggerMaxScore)
            {
                var severity = answer.Score == 1 ? 2 : 1;
                actions.Add(Score(t, answer.Dimension, severity, bottlenecks, cbamRiskHigh,
                    firstReason: $"Soru {answer.QuestionNumber} yanıtınız: {answer.Score}/5",
                    firstSimpleReason: answer.AnswerLabel is { } label
                        ? $"Cevabınız: \"{label}\""
                        : $"Soru {answer.QuestionNumber} cevabınız zayıf görünüyor",
                    questionNumber: answer.QuestionNumber, answerScore: answer.Score, answerLabel: answer.AnswerLabel,
                    tieBreakValue: answer.Score));
            }
            else if (t.TriggerType == RecommendationTrigger.Dimension
                     && t.Dimension is { } dim
                     && dimensionScores.TryGetValue(dim, out var dimScore)
                     && dimScore < DimensionTriggerThreshold)
            {
                var severity = dimScore < SevereDimensionThreshold ? 2 : 1;
                strategic.Add(Score(t, dim, severity, bottlenecks, cbamRiskHigh,
                    firstReason: $"{ScoreCalculator.DisplayName(dim)} skorunuz {dimScore:0.##} (eşik: {DimensionTriggerThreshold:0})",
                    firstSimpleReason: $"{ScoreCalculator.SimpleName(dim)} puanınız 100 üzerinden " +
                                       $"{Math.Round(dimScore, 0, MidpointRounding.AwayFromZero)}",
                    questionNumber: null, answerScore: null, answerLabel: null,
                    tieBreakValue: dimScore));
            }
        }

        var result = new List<AssessmentRecommendation>();
        result.AddRange(Rank(actions, markTop: true, showCbamNotes));
        result.AddRange(Rank(strategic, markTop: false, showCbamNotes));
        return result;
    }

    /// <summary>En düşük skorlu boyut(lar). Eşitlik varsa hepsi darboğaz sayılır.</summary>
    public static IReadOnlySet<Dimension> FindBottlenecks(IReadOnlyDictionary<Dimension, decimal> dimensionScores)
    {
        if (dimensionScores.Count == 0)
            return new HashSet<Dimension>();

        var min = dimensionScores.Values.Min();
        return dimensionScores.Where(kv => kv.Value == min).Select(kv => kv.Key).ToHashSet();
    }

    // ------------------------------------------------------------------

    private sealed record Candidate(
        Recommendation Template,
        Dimension Dimension,
        int Urgency,
        Level EffectivePriority,
        List<string> Reasons,
        List<string> SimpleReasons,
        int? QuestionNumber,
        int? AnswerScore,
        string? AnswerLabel,
        decimal TieBreakValue);

    private static Candidate Score(
        Recommendation t, Dimension dimension, int severity, IReadOnlySet<Dimension> bottlenecks, bool cbamRiskHigh,
        string firstReason, string firstSimpleReason, int? questionNumber, int? answerScore, string? answerLabel,
        decimal tieBreakValue)
    {
        // Her gerekçe iki dilde üretilir: teknik (danışman) ve sade (KOBİ sahibi).
        var reasons = new List<string> { firstReason };
        var simple = new List<string> { firstSimpleReason };

        var urgency = (int)t.ImpactLevel * 2      // etki: 2/4/6
                      + (4 - (int)t.CostLevel)    // maliyet: düşük 3, orta 2, yüksek 1
                      + ((int)t.Priority - 1)     // öncelik: 0/1/2
                      + severity;                 // ciddiyet: 1/2

        if (bottlenecks.Contains(dimension))
        {
            urgency += BottleneckBonus;
            reasons.Add($"En zayıf boyutunuz: {ScoreCalculator.DisplayName(dimension)}");
            simple.Add($"En çok zorlandığınız konu: {ScoreCalculator.SimpleName(dimension)}");
        }

        var priority = t.Priority;
        if (cbamRiskHigh && t.CbamRelevant)
        {
            urgency += CbamBonus;
            priority = Level.High;
            reasons.Add("CBAM riskiniz yüksek");
            simple.Add("Avrupa'ya sattığınız ürünler karbon kurallarından etkileniyor");
        }

        if (t.CostLevel == Level.Low)
        {
            reasons.Add("Düşük maliyet");
            simple.Add("Az masraflı");
        }

        if (t.ImpactLevel == Level.High)
        {
            reasons.Add("Yüksek etki");
            simple.Add("Etkisi büyük");
        }

        return new Candidate(t, dimension, urgency, priority, reasons, simple, questionNumber, answerScore, answerLabel,
            tieBreakValue);
    }

    private static IEnumerable<AssessmentRecommendation> Rank(List<Candidate> candidates, bool markTop, bool showCbamNotes)
    {
        var ordered = candidates
            .OrderByDescending(c => c.Urgency)
            .ThenBy(c => c.TieBreakValue)              // daha düşük yanıt / skor önce
            .ThenBy(c => (int)c.Template.CostLevel)    // daha ucuz önce
            .ThenBy(c => c.QuestionNumber ?? 0)
            .ThenBy(c => (int)c.Dimension)
            .ToList();

        return ordered.Select((c, i) => new AssessmentRecommendation
        {
            RecommendationId = c.Template.Id,
            TriggerType = c.Template.TriggerType,
            Dimension = c.Dimension,
            Rank = i + 1,
            IsTopPriority = markTop && i < TopActionCount,
            UrgencyScore = c.Urgency,
            QuestionNumber = c.QuestionNumber,
            AnswerScore = c.AnswerScore,
            Title = c.Template.Title,
            Rationale = c.Template.Rationale,
            CbamNote = showCbamNotes ? c.Template.CbamNote : null,
            SimpleTitle = c.Template.SimpleTitle,
            SimpleBenefit = c.Template.SimpleBenefit,
            SimpleCbamNote = showCbamNotes ? c.Template.SimpleCbamNote : null,
            AnswerLabel = c.AnswerLabel,
            Priority = c.EffectivePriority,
            CostLevel = c.Template.CostLevel,
            ImpactLevel = c.Template.ImpactLevel,
            ImpactType = c.Template.ImpactType,
            DurationMinMonths = c.Template.DurationMinMonths,
            DurationMaxMonths = c.Template.DurationMaxMonths,
            Reasons = string.Join('\n', c.Reasons),
            SimpleReasons = string.Join('\n', c.SimpleReasons)
        });
    }
}
