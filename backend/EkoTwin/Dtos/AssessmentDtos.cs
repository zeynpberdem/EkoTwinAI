using EkoTwin.Services;
using System.ComponentModel.DataAnnotations;
using EkoTwin.Models;

namespace EkoTwin.Dtos;

/// <summary>Yeni değerlendirme: firma + 16 sorunun yanıtları.</summary>
public class CreateAssessmentRequest
{
    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "En az bir yanıt gönderilmelidir.")]
    public List<AnswerRequest> Answers { get; set; } = new();
}

/// <summary>Tek bir sorunun yanıtı.</summary>
public class AnswerRequest
{
    [Required]
    public Guid QuestionId { get; set; }

    [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
    public int Score { get; set; }
}

/// <summary>Bir boyutun skoru (0-100).</summary>
public record DimensionScoreResponse(
    Dimension Dimension,
    string DimensionName,
    string SimpleName,
    decimal Score,
    string MaturityLevel,
    string SimpleMaturityLevel,
    int MaturityStage);

/// <summary>Soru bazında verilen yanıt.</summary>
public record AnswerResponse(
    Guid QuestionId,
    int DisplayOrder,
    Dimension Dimension,
    string QuestionText,
    string SimpleText,
    int Score,
    string? AnswerLabel);

/// <summary>SKDM / CBAM risk değerlendirmesi.</summary>
public record CbamRiskResponse(
    bool IsInScope,
    CbamProductGroup ProductGroup,
    string ProductGroupName,
    string RiskLevel,
    string Summary,
    IReadOnlyList<string> Actions,
    string SimpleRiskLevel,
    string SimpleSummary,
    IReadOnlyList<string> SimpleActions);

/// <summary>Değerlendirme sonucu (rapor).</summary>
public record AssessmentResponse(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    DateTime CreatedAt,
    decimal TotalScore,
    string MaturityLevel,
    string SimpleMaturityLevel,
    int MaturityStage,
    IReadOnlyList<DimensionScoreResponse> DimensionScores,
    CbamRiskResponse CbamRisk,
    RecommendationReportResponse Recommendations,
    IReadOnlyList<AnswerResponse> Answers);

/// <summary>Firmanın geçmiş değerlendirmelerini listelemek için özet.</summary>
public record AssessmentSummaryResponse(
    Guid Id,
    DateTime CreatedAt,
    decimal TotalScore,
    string MaturityLevel,
    string SimpleMaturityLevel,
    int MaturityStage,
    decimal DigitalInfrastructureScore,
    decimal EnergyEfficiencyScore,
    decimal CarbonManagementScore,
    decimal SupplyChainScore)
{
    public static AssessmentSummaryResponse From(Assessment a) => new(
        a.Id,
        a.CreatedAt,
        a.TotalScore,
        ScoreCalculator.MaturityLevel(a.TotalScore),
        ScoreCalculator.SimpleMaturityLevel(a.TotalScore),
        ScoreCalculator.MaturityStage(a.TotalScore),
        a.DigitalInfrastructureScore,
        a.EnergyEfficiencyScore,
        a.CarbonManagementScore,
        a.SupplyChainScore);
}
