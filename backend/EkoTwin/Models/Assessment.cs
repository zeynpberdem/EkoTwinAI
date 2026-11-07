using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Bir firmanın ikiz dönüşüm değerlendirmesi (test sonucu).
/// Skorlar 0-100 arasıdır ve yanıtlardan (AssessmentDetails) hesaplanıp kaydedilir.
/// </summary>
public class Assessment
{
    /// <summary>Birincil anahtar (UUID v7).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Değerlendirmenin ait olduğu firma (foreign key).</summary>
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    /// <summary>Değerlendirme tarihi (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Genel ikiz dönüşüm skoru (0-100).</summary>
    public decimal TotalScore { get; set; }

    /// <summary>Dijital Altyapı skoru (0-100).</summary>
    public decimal DigitalInfrastructureScore { get; set; }

    /// <summary>Enerji Verimliliği skoru (0-100).</summary>
    public decimal EnergyEfficiencyScore { get; set; }

    /// <summary>Karbon Yönetimi skoru (0-100).</summary>
    public decimal CarbonManagementScore { get; set; }

    /// <summary>Tedarik Zinciri skoru (0-100).</summary>
    public decimal SupplyChainScore { get; set; }

    /// <summary>Soru bazında verilen yanıtlar.</summary>
    public ICollection<AssessmentDetail> AssessmentDetails { get; set; } = new List<AssessmentDetail>();
}
