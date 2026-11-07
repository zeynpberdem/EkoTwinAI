namespace EkoTwin.Models;

/// <summary>
/// Standart sektör listesi (Elazığ ve Anadolu sanayisinde yaygın kollar).
/// Serbest metin yerine liste kullanılması, sektörel karşılaştırmayı mümkün kılar
/// ("Tekstil", "tekstil", "TEKSTİL" aynı sektör sayılır).
/// Ayrıntı gerekiyorsa <see cref="Company.SubSector"/> alanına serbest metin yazılır.
/// Veritabanında metin olarak saklanır; Türkçe adları <c>Lookups.SectorName</c> verir.
/// </summary>
public enum SectorType
{
    MetalWorkingAndCasting = 1,
    MachineryAndEquipment = 2,
    AutomotiveSupply = 3,
    FurnitureAndWood = 4,
    FoodAndBeverage = 5,
    TextileAndApparel = 6,
    MiningAndMarble = 7,
    ConstructionMaterials = 8,
    ChemicalsAndPlastics = 9,
    ElectricalAndElectronics = 10,
    Energy = 11,
    Other = 99
}
