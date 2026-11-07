using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Bir değerlendirmede üretilen önerinin KAYDI (anlık görüntü / snapshot).
/// Öneri metni ve puanlaması üretildiği andaki haliyle saklanır; öneri şablonu sonradan
/// değiştirilse bile eski raporlar değişmez (gelişim takibi için gerekli).
/// </summary>
public class AssessmentRecommendation
{
    [Key]
    public Guid Id { get; set; }

    public Guid AssessmentId { get; set; }

    public Assessment Assessment { get; set; } = null!;

    /// <summary>Kaynak şablon (şablon silinirse boş kalır, kayıt korunur).</summary>
    public Guid? RecommendationId { get; set; }

    public RecommendationTrigger TriggerType { get; set; }

    /// <summary>Önerinin ait olduğu boyut (soru bazlıysa sorunun boyutu).</summary>
    public Dimension Dimension { get; set; }

    /// <summary>Kendi grubu içindeki sıra (aksiyonlar ve stratejik uyarılar ayrı sıralanır), 1'den başlar.</summary>
    public int Rank { get; set; }

    /// <summary>"En acil 3 adım" içinde mi?</summary>
    public bool IsTopPriority { get; set; }

    public int UrgencyScore { get; set; }

    /// <summary>Soru bazlıysa soru numarası (1-16).</summary>
    public int? QuestionNumber { get; set; }

    /// <summary>Soru bazlıysa verilen puan (1-2).</summary>
    public int? AnswerScore { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Rationale { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? CbamNote { get; set; }

    /// <summary>Firmaya göre ayarlanmış öncelik.</summary>
    public Level Priority { get; set; }

    public Level CostLevel { get; set; }

    public Level ImpactLevel { get; set; }

    public ImpactType ImpactType { get; set; }

    public int DurationMinMonths { get; set; }

    public int DurationMaxMonths { get; set; }

    /// <summary>Neden bu sırada olduğu; satır satır (\n ile ayrılmış).</summary>
    [MaxLength(1000)]
    public string Reasons { get; set; } = string.Empty;
}
