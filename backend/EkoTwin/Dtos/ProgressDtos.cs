using System.ComponentModel.DataAnnotations;
using EkoTwin.Models;

namespace EkoTwin.Dtos;

// ======================= Değerlendirme karşılaştırması =======================

/// <summary>Bir önceki önerinin yeni değerlendirmedeki durumu.</summary>
public enum FollowUpStatus
{
    /// <summary>Öneriyi tetikleyen koşul artık yok (soru ≥ 3 puan veya boyut skoru ≥ 50).</summary>
    Improved = 1,

    /// <summary>Koşul devam ediyor.</summary>
    StillOpen = 2,

    /// <summary>Önceki raporda yoktu, bu sefer çıktı (bir alan gerilemiş).</summary>
    New = 3
}

public record DimensionChangeResponse(
    Dimension Dimension,
    string DimensionName,
    decimal PreviousScore,
    decimal CurrentScore,
    decimal Change);

public record QuestionChangeResponse(
    int QuestionNumber,
    string QuestionText,
    Dimension Dimension,
    int PreviousScore,
    int CurrentScore,
    int Change);

public record FollowUpItemResponse(
    string Title,
    RecommendationTrigger TriggerType,
    Dimension Dimension,
    string DimensionName,
    int? QuestionNumber,
    bool WasTopPriority,
    FollowUpStatus Status,
    string StatusName,
    string Detail);

public record FollowUpSummaryResponse(
    int PreviousCount,
    int ImprovedCount,
    int StillOpenCount,
    int NewCount,
    int TopPriorityCount,
    int TopPriorityImprovedCount,
    string Summary);

public record RecommendationFollowUpResponse(
    FollowUpSummaryResponse Summary,
    IReadOnlyList<FollowUpItemResponse> Items);

/// <summary>İki değerlendirmenin karşılaştırması ("Geçen sefere göre ne değişti?").</summary>
public record AssessmentComparisonResponse(
    AssessmentSummaryResponse Previous,
    AssessmentSummaryResponse Current,
    int DaysBetween,
    decimal TotalChange,
    bool MaturityLevelChanged,
    string? MaturityChange,
    IReadOnlyList<DimensionChangeResponse> DimensionChanges,
    Dimension? MostImproved,
    string? MostImprovedName,
    Dimension? MostDeclined,
    string? MostDeclinedName,
    IReadOnlyList<QuestionChangeResponse> QuestionChanges,
    RecommendationFollowUpResponse RecommendationFollowUp);

// ======================= Karbon hedefi =======================

public class CarbonTargetRequest
{
    /// <summary>Baz yıl (bu yıl için karbon hesabı yapılmış olmalı).</summary>
    [Range(2000, 2100, ErrorMessage = "Baz yıl 2000 ile 2100 arasında olmalıdır.")]
    public int BaseYear { get; set; }

    /// <summary>Hedef yıl (baz yıldan sonra).</summary>
    [Range(2001, 2100, ErrorMessage = "Hedef yıl 2001 ile 2100 arasında olmalıdır.")]
    public int TargetYear { get; set; }

    /// <summary>Baz yıla göre azaltma oranı (%).</summary>
    // Sınırlar kültürden bağımsız okunur; aksi halde Türkçe Windows'ta "0.01" geçersiz sayı sayılır (500 hatası).
    [Range(typeof(decimal), "0.01", "100", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Azaltma oranı 0 ile 100 arasında olmalıdır.")]
    public decimal ReductionPercent { get; set; }
}

public enum TargetStatus
{
    /// <summary>Baz yıl için karbon kaydı yok (silinmiş olabilir).</summary>
    BaseYearMissing = 1,

    /// <summary>Baz yıldan sonraki bir yıl için henüz kayıt yok.</summary>
    NoDataAfterBaseYear = 2,

    Achieved = 3,
    OnTrack = 4,
    Behind = 5
}

/// <summary>Karbon hedefi ve ilerlemesi.</summary>
public record CarbonTargetResponse(
    Guid Id,
    int BaseYear,
    int TargetYear,
    decimal ReductionPercent,
    decimal? BaseYearTonnes,
    decimal? TargetTonnes,
    int? LatestYear,
    decimal? LatestTonnes,
    decimal? AchievedReductionPercent,
    decimal? ProgressPercent,
    decimal? ExpectedReductionPercent,
    int? ProjectedYear,
    TargetStatus Status,
    string StatusName,
    string Summary,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// ======================= Gelişim özeti =======================

/// <summary>Yıllara göre karbon emisyonu ve hedef ilerlemesi.</summary>
public record CarbonTrendResponse(
    IReadOnlyList<CarbonFootprintSummaryResponse> Series,
    int? BaseYear,
    int? LatestYear,
    decimal? ChangeSinceBasePercent,
    decimal? PerEmployeeChangeSinceBasePercent,
    CarbonTargetResponse? Target);

/// <summary>
/// Firmanın gelişim sayfası: değerlendirme geçmişi, son iki değerlendirmenin karşılaştırması,
/// öneri takibi, karbon trendi, hedef ilerlemesi ve sade dilde özet.
/// </summary>
public record ProgressResponse(
    Guid CompanyId,
    string CompanyName,
    int AssessmentCount,
    IReadOnlyList<AssessmentSummaryResponse> Assessments,
    AssessmentComparisonResponse? LatestComparison,
    int? DaysSinceLastAssessment,
    bool ReassessmentDue,
    CarbonTrendResponse Carbon,
    IReadOnlyList<string> Highlights);
