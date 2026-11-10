using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>Raporun anlatım metnini kim üretti?</summary>
public enum NarrativeSource
{
    /// <summary>Yapay zekâ yazdı ve otomatik kontrollerden geçti.</summary>
    Ai = 1,

    /// <summary>Kural tabanlı şablon (yapay zekâ ayarlı değil, yanıt vermedi veya kontrolden geçemedi).</summary>
    Template = 2
}

/// <summary>
/// Bir değerlendirme için üretilen, KOBİ sahibine hitap eden sade dilli rapor mektubu.
/// Her üretim saklanır (snapshot); hangi model ve hangi talimat sürümüyle üretildiği kayıtlıdır.
/// </summary>
public class AssessmentNarrative
{
    public Guid Id { get; set; }

    public Guid AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;

    [MaxLength(6000)]
    public string Text { get; set; } = string.Empty;

    public NarrativeSource Source { get; set; }

    /// <summary>Yapay zekâ sağlayıcısı (örn. "gemini"); şablonda "template".</summary>
    [MaxLength(30)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>Kullanılan model adı (yapay zekâ denendiyse).</summary>
    [MaxLength(100)]
    public string? Model { get; set; }

    /// <summary>Talimat (prompt) sürümü; talimat değişince eski metinler hangi kurallarla üretildiğini korur.</summary>
    [MaxLength(20)]
    public string PromptVersion { get; set; } = string.Empty;

    /// <summary>Şablona düşüldüyse nedeni (örn. "Metinde bilgi kartında olmayan sayı var: 35").</summary>
    [MaxLength(1000)]
    public string? FallbackReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
