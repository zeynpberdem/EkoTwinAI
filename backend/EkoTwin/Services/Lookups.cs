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
}
