namespace EkoTwin.Models;

/// <summary>
/// Bir firmanın bir yıla ait kurumsal karbon ayak izi (Kapsam 1 + Kapsam 2).
/// Her firma için yıl başına tek kayıt tutulur; yıllar karşılaştırılarak gelişim izlenir.
/// </summary>
public class CarbonFootprint
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>Verilerin ait olduğu yıl (örn. 2025 faturaları).</summary>
    public int Year { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Kapsam 1 (doğrudan) emisyon, ton CO2e.</summary>
    public decimal Scope1Tonnes { get; set; }

    /// <summary>Kapsam 2 (elektrik) emisyon, ton CO2e.</summary>
    public decimal Scope2Tonnes { get; set; }

    /// <summary>Toplam emisyon, ton CO2e.</summary>
    public decimal TotalTonnes { get; set; }

    /// <summary>Hesap anındaki çalışan sayısı (çalışan başına emisyon için saklanır).</summary>
    public int EmployeeCount { get; set; }

    /// <summary>Çalışan başına ton CO2e.</summary>
    public decimal TonnesPerEmployee { get; set; }

    /// <summary>Kaynak bazında girdiler ve sonuçlar.</summary>
    public ICollection<CarbonFootprintItem> Items { get; set; } = new List<CarbonFootprintItem>();
}
