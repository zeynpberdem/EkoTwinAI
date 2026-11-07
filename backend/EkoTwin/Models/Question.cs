using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Anket sorusu. Toplam 16 soru vardır (4 boyut × 4 soru).
/// Her sorunun 5 anlamlı cevap seçeneği vardır; seçilen seçeneğin puanı (1-5) saklanır.
/// </summary>
public class Question
{
    /// <summary>Birincil anahtar (UUID v7).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Sorunun ait olduğu dönüşüm boyutu.</summary>
    public Dimension Dimension { get; set; }

    /// <summary>Resmi / teknik soru metni (danışman ve akademik rapor için).</summary>
    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    /// <summary>KOBİ sahibine gösterilen sade soru metni.</summary>
    [Required]
    [MaxLength(300)]
    public string SimpleText { get; set; } = string.Empty;

    /// <summary>"Bu ne demek?" açıklaması (arayüzde ⓘ simgesiyle açılır). İsteğe bağlı.</summary>
    [MaxLength(500)]
    public string? HelpText { get; set; }

    /// <summary>Ankette gösterilme sırası (1-16, benzersiz).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Pasif sorular yeni değerlendirmelerde gösterilmez; eski yanıtlar korunur.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Cevap seçenekleri (puan 1-5).</summary>
    public ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();

    /// <summary>Bu soruya verilmiş yanıtlar.</summary>
    public ICollection<AssessmentDetail> AssessmentDetails { get; set; } = new List<AssessmentDetail>();
}
