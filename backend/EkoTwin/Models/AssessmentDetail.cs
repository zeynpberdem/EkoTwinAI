using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Bir değerlendirmede tek bir soruya verilen yanıt.
/// Bir değerlendirmede her soru yalnızca bir kez yanıtlanabilir.
/// </summary>
public class AssessmentDetail
{
    /// <summary>Birincil anahtar (UUID v7).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Yanıtın ait olduğu değerlendirme (foreign key).</summary>
    public Guid AssessmentId { get; set; }

    public Assessment Assessment { get; set; } = null!;

    /// <summary>Yanıtlanan soru (foreign key).</summary>
    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    /// <summary>Verilen puan: 1 (Hiç / Yok) – 5 (Tamamen / İleri düzeyde).</summary>
    [Range(1, 5)]
    public int Score { get; set; }
}
