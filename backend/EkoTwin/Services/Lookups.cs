using EkoTwin.Models;

namespace EkoTwin.Services;

/// <summary>
/// Enum değerlerinin Türkçe adları. Arayüzdeki açılır listeler ve raporlar bu adları kullanır.
/// Tüm çeviriler tek yerde toplanır.
/// </summary>
public static class Lookups
{
    public static string SectorName(SectorType sector) => sector switch
    {
        SectorType.MetalWorkingAndCasting => "Metal İşleme ve Döküm",
        SectorType.MachineryAndEquipment => "Makine ve Ekipman",
        SectorType.AutomotiveSupply => "Otomotiv Yan Sanayi",
        SectorType.FurnitureAndWood => "Mobilya ve Ağaç Ürünleri",
        SectorType.FoodAndBeverage => "Gıda ve İçecek",
        SectorType.TextileAndApparel => "Tekstil ve Hazır Giyim",
        SectorType.MiningAndMarble => "Madencilik ve Mermer",
        SectorType.ConstructionMaterials => "Yapı Malzemeleri",
        SectorType.ChemicalsAndPlastics => "Kimya ve Plastik",
        SectorType.ElectricalAndElectronics => "Elektrik ve Elektronik",
        SectorType.Energy => "Enerji",
        SectorType.Other => "Diğer",
        _ => sector.ToString()
    };

    public static string DimensionName(Dimension dimension) => ScoreCalculator.DisplayName(dimension);

    public static string LevelName(Level level) => level switch
    {
        Level.Low => "Düşük",
        Level.Medium => "Orta",
        Level.High => "Yüksek",
        _ => level.ToString()
    };

    public static string ImpactTypeName(ImpactType type) => type switch
    {
        ImpactType.QuickWin => "Hızlı Kazanım",
        ImpactType.StrategicCompliance => "Stratejik Uyum",
        ImpactType.LongTermEfficiency => "Uzun Vadeli Verimlilik",
        _ => type.ToString()
    };

    /// <summary>"1-3 ay" veya aynıysa "1 ay".</summary>
    public static string DurationText(int minMonths, int maxMonths) =>
        minMonths == maxMonths ? $"{minMonths} ay" : $"{minMonths}-{maxMonths} ay";

    public static string CbamProductGroupName(CbamProductGroup group) => CbamRiskEvaluator.DisplayName(group);

    public static string EmissionSourceName(EmissionSource source) => source switch
    {
        EmissionSource.Electricity => "Elektrik (şebekeden)",
        EmissionSource.NaturalGas => "Doğal gaz",
        EmissionSource.Diesel => "Motorin (dizel)",
        EmissionSource.Gasoline => "Benzin",
        EmissionSource.Lpg => "LPG",
        EmissionSource.Coke => "Kok kömürü",
        EmissionSource.Lignite => "Linyit",
        _ => source.ToString()
    };

    public static string EmissionScopeName(EmissionScope scope) => scope switch
    {
        EmissionScope.Scope1 => "Kapsam 1 (doğrudan)",
        EmissionScope.Scope2 => "Kapsam 2 (satın alınan elektrik)",
        _ => scope.ToString()
    };
}
