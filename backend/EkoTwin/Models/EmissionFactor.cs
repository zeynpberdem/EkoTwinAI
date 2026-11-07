using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Bir enerji/yakıt kaynağının emisyon faktörü (birim başına kg CO2e) ve kaynağı.
/// Değerler veritabanında tutulur; resmi kaynak güncellendiğinde yeni migration ile değiştirilir.
/// Eski hesaplar etkilenmez çünkü her hesap kullandığı faktörü kendi içinde saklar.
/// </summary>
public class EmissionFactor
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Hangi kaynak (her kaynak için tek faktör).</summary>
    public EmissionSource Source { get; set; }

    /// <summary>Kapsam 1 (doğrudan) veya Kapsam 2 (elektrik).</summary>
    public EmissionScope Scope { get; set; }

    /// <summary>Girdi birimi: "kWh", "m³", "litre", "kg", "ton".</summary>
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>Birim başına kg CO2e (CO2 + CH4 + N2O, IPCC AR5 küresel ısınma potansiyelleriyle).</summary>
    public decimal KgCo2ePerUnit { get; set; }

    /// <summary>Faktörün kaynağı ve hesaplama varsayımları.</summary>
    [Required]
    [MaxLength(500)]
    public string Reference { get; set; } = string.Empty;

    /// <summary>Faktörün ait olduğu veri yılı (örn. elektrik için 2022).</summary>
    public int ReferenceYear { get; set; }

    public bool IsActive { get; set; } = true;
}
