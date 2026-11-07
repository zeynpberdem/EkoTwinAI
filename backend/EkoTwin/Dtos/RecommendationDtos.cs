using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Dtos;

/// <summary>
/// Raporda tek bir öneri. Arayüz KOBİ sahibine Simple* alanlarını gösterir
/// (sade başlık, ne kazandırır, sade gerekçeler); teknik alanlar danışman görünümü ve PDF içindir.
/// </summary>
public record RecommendationResponse(
    int Rank,
    bool IsTopPriority,
    RecommendationTrigger TriggerType,
    Dimension Dimension,
    string DimensionName,
    string DimensionSimpleName,
    int? QuestionNumber,
    int? AnswerScore,
    string? AnswerLabel,
    string SimpleTitle,
    string SimpleBenefit,
    string? SimpleCbamNote,
    string SimpleCostText,
    IReadOnlyList<string> SimpleReasons,
    string Title,
    string Rationale,
    string? CbamNote,
    Level Priority,
    string PriorityName,
    Level CostLevel,
    string CostLevelName,
    Level ImpactLevel,
    string ImpactLevelName,
    ImpactType ImpactType,
    string ImpactTypeName,
    int DurationMinMonths,
    int DurationMaxMonths,
    string DurationText,
    int UrgencyScore,
    IReadOnlyList<string> Reasons)
{
    public static RecommendationResponse From(AssessmentRecommendation r) => new(
        r.Rank,
        r.IsTopPriority,
        r.TriggerType,
        r.Dimension,
        Lookups.DimensionName(r.Dimension),
        ScoreCalculator.SimpleName(r.Dimension),
        r.QuestionNumber,
        r.AnswerScore,
        r.AnswerLabel,
        // Sade alanlar eklenmeden önce üretilmiş eski raporlarda teknik başlığa düşülür.
        string.IsNullOrEmpty(r.SimpleTitle) ? r.Title : r.SimpleTitle,
        r.SimpleBenefit,
        r.SimpleCbamNote,
        Lookups.SimpleCostText(r.CostLevel),
        r.SimpleReasons.Split('\n', StringSplitOptions.RemoveEmptyEntries),
        r.Title,
        r.Rationale,
        r.CbamNote,
        r.Priority,
        Lookups.LevelName(r.Priority),
        r.CostLevel,
        Lookups.LevelName(r.CostLevel),
        r.ImpactLevel,
        Lookups.LevelName(r.ImpactLevel),
        r.ImpactType,
        Lookups.ImpactTypeName(r.ImpactType),
        r.DurationMinMonths,
        r.DurationMaxMonths,
        Lookups.DurationText(r.DurationMinMonths, r.DurationMaxMonths),
        r.UrgencyScore,
        r.Reasons.Split('\n', StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>Değerlendirme raporunun öneri bölümü.</summary>
public record RecommendationReportResponse(
    Dimension? BottleneckDimension,
    string? BottleneckDimensionName,
    IReadOnlyList<RecommendationResponse> TopActions,
    IReadOnlyList<RecommendationResponse> Actions,
    IReadOnlyList<RecommendationResponse> StrategicAlerts);

/// <summary>Öneri kütüphanesindeki bir şablon (inceleme / ileride admin paneli için).</summary>
public record RecommendationTemplateResponse(
    Guid Id,
    RecommendationTrigger TriggerType,
    int? QuestionNumber,
    Dimension Dimension,
    string DimensionName,
    string Title,
    string Rationale,
    string? CbamNote,
    string SimpleTitle,
    string SimpleBenefit,
    string? SimpleCbamNote,
    string PriorityName,
    string CostLevelName,
    string ImpactLevelName,
    string ImpactTypeName,
    string DurationText,
    string? Source,
    bool IsActive);
