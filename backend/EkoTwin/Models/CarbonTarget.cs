namespace EkoTwin.Models;

/// <summary>
/// Firmanın karbon azaltım hedefi (örn. "2024'e göre 2030'a kadar %20 azaltma").
/// Her firmanın tek bir aktif hedefi olur; hedef mutlak toplam emisyon (Kapsam 1 + 2) üzerinden izlenir.
/// </summary>
public class CarbonTarget
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>Karşılaştırmanın başladığı yıl (bu yıl için karbon kaydı olmalıdır).</summary>
    public int BaseYear { get; set; }

    /// <summary>Hedefe ulaşılması planlanan yıl.</summary>
    public int TargetYear { get; set; }

    /// <summary>Baz yıla göre azaltma oranı (%), örn. 20.</summary>
    public decimal ReductionPercent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
