namespace EkoTwin.Models;

/// <summary>
/// İkiz (dijital + yeşil) dönüşümün incelendiği 4 temel boyut.
/// Veritabanında okunabilir olması için metin olarak saklanır (örn. "EnergyEfficiency").
/// </summary>
public enum Dimension
{
    /// <summary>Dijital Altyapı</summary>
    DigitalInfrastructure = 1,

    /// <summary>Enerji Verimliliği</summary>
    EnergyEfficiency = 2,

    /// <summary>Karbon Yönetimi</summary>
    CarbonManagement = 3,

    /// <summary>Tedarik Zinciri</summary>
    SupplyChain = 4
}
