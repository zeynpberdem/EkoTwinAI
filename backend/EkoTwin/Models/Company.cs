using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// İkiz dönüşüm değerlendirmesine tabi tutulan KOBİ'yi temsil eder.
/// </summary>
public class Company
{
    /// <summary>Birincil anahtar (UUID). PostgreSQL 18'in uuidv7() fonksiyonuyla zaman sıralı üretilir.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Firmanın ticari unvanı.</summary>
    [Required]
    [MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Firmanın faaliyet gösterdiği sektör (standart listeden).</summary>
    public SectorType Sector { get; set; }

    /// <summary>İsteğe bağlı alt sektör / ayrıntı (örn. "Alüminyum döküm", "Ofis mobilyası").</summary>
    [MaxLength(100)]
    public string SubSector { get; set; } = string.Empty;

    /// <summary>Çalışan sayısı.</summary>
    public int EmployeeCount { get; set; }

    /// <summary>Firma AB ülkelerine ihracat yapıyor mu? (CBAM risk değerlendirmesi için)</summary>
    public bool ExportsToEu { get; set; }

    /// <summary>AB'ye satılan ana ürünün CBAM ürün grubu; kapsam dışıysa None.</summary>
    public CbamProductGroup CbamProductGroup { get; set; } = CbamProductGroup.None;

    /// <summary>Kayıt tarihi (UTC). Npgsql, timestamptz kolonları için UTC değer bekler.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Firmanın yaptığı değerlendirmeler (bire-çok).</summary>
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}
