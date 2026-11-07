using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Karbon ayak izindeki tek bir kaynak satırı (örn. "Elektrik: 250.000 kWh → 119,5 t CO2e").
/// Kullanılan faktör ve kaynağı burada da saklanır (snapshot): faktör tablosu güncellense bile
/// bu hesap aynı kalır ve nasıl yapıldığı denetlenebilir.
/// </summary>
public class CarbonFootprintItem
{
    public Guid Id { get; set; }

    public Guid CarbonFootprintId { get; set; }
    public CarbonFootprint CarbonFootprint { get; set; } = null!;

    public EmissionSource Source { get; set; }
    public EmissionScope Scope { get; set; }

    /// <summary>Girilen miktar (birimi <see cref="Unit"/>).</summary>
    public decimal Quantity { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>Hesapta kullanılan faktör (birim başına kg CO2e).</summary>
    public decimal FactorKgCo2ePerUnit { get; set; }

    [MaxLength(500)]
    public string FactorReference { get; set; } = string.Empty;

    /// <summary>Bu kaynaktan doğan emisyon, ton CO2e.</summary>
    public decimal EmissionTonnes { get; set; }
}
