using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Anket sorusu. Toplam 16 soru vardır (4 boyut × 4 soru).
/// Her soru 1-5 arası puanlanır: 1 = Hiç / Yok, 5 = Tamamen / İleri düzeyde.
/// </summary>
public class Question
{
    /// <summary>Birincil anahtar (UUID v7).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Sorunun ait olduğu dönüşüm boyutu.</summary>
    public Dimension Dimension { get; set; }

    /// <summary>Soru metni.</summary>
    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    /// <summary>Ankette gösterilme sırası (1-16, benzersiz).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Pasif sorular yeni değerlendirmelerde gösterilmez; eski yanıtlar korunur.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Bu soruya verilmiş yanıtlar.</summary>
    public ICollection<AssessmentDetail> AssessmentDetails { get; set; } = new List<AssessmentDetail>();
}
