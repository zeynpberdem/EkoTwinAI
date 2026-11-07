using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Öneri şablonu (öneri kütüphanesi). İki türü vardır:
/// - Soru bazlı: <see cref="QuestionId"/> dolu; o soruya 1-2 puan verilirse tetiklenir.
/// - Boyut bazlı: <see cref="Dimension"/> dolu; boyut skoru 50'nin altındaysa tetiklenir.
/// Başlangıç önerileri Data/RecommendationSeed.cs ile eklenir; ileride admin panelinden düzenlenebilir.
/// </summary>
public class Recommendation
{
    [Key]
    public Guid Id { get; set; }

    public RecommendationTrigger TriggerType { get; set; }

    /// <summary>Soru bazlı önerilerde tetikleyen soru.</summary>
    public Guid? QuestionId { get; set; }

    public Question? Question { get; set; }

    /// <summary>Boyut bazlı önerilerde tetikleyen boyut.</summary>
    public Dimension? Dimension { get; set; }

    /// <summary>Ne yapılacak (kısa başlık).</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Neden önemli (firmaya sade dille açıklama).</summary>
    [Required]
    [MaxLength(1000)]
    public string Rationale { get; set; } = string.Empty;

    /// <summary>Temel öncelik. CBAM riski yüksek firmalarda CBAM ilişkili öneriler Yüksek'e çıkarılır.</summary>
    public Level Priority { get; set; }

    public Level CostLevel { get; set; }

    /// <summary>Etkinin büyüklüğü (sıralamada kullanılır).</summary>
    public Level ImpactLevel { get; set; }

    /// <summary>Etkinin türü (raporda etiket).</summary>
    public ImpactType ImpactType { get; set; }

    public int DurationMinMonths { get; set; }

    public int DurationMaxMonths { get; set; }

    /// <summary>CBAM ile ilişkili mi? (CBAM riski yüksekse aciliyet puanı artar)</summary>
    public bool CbamRelevant { get; set; }

    /// <summary>Yalnızca AB'ye CBAM kapsamında ürün ihraç eden firmalara gösterilen ek not.</summary>
    [MaxLength(500)]
    public string? CbamNote { get; set; }

    /// <summary>KOBİ sahibine gösterilen sade başlık.</summary>
    [MaxLength(150)]
    public string SimpleTitle { get; set; } = string.Empty;

    /// <summary>"Ne kazandırır?" — tek-iki kısa cümle, sade dil.</summary>
    [MaxLength(300)]
    public string SimpleBenefit { get; set; } = string.Empty;

    /// <summary>CBAM notunun sade hâli (yalnızca CbamNote doluysa).</summary>
    [MaxLength(300)]
    public string? SimpleCbamNote { get; set; }

    /// <summary>Metindeki iddiaların kaynağı (varsa).</summary>
    [MaxLength(500)]
    public string? Source { get; set; }

    public bool IsActive { get; set; } = true;
}
