namespace EkoTwin.Models;

/// <summary>
/// Karbon hesaplayıcıdaki enerji/yakıt kaynakları. KOBİ'nin faturalarından okuyabileceği değerler seçildi.
/// </summary>
public enum EmissionSource
{
    /// <summary>Şebekeden satın alınan elektrik (kWh) → Kapsam 2</summary>
    Electricity = 1,

    /// <summary>Doğal gaz (Sm³ = faturadaki m³) → Kapsam 1</summary>
    NaturalGas = 2,

    /// <summary>Motorin / dizel (litre): jeneratör, forklift, firma araçları → Kapsam 1</summary>
    Diesel = 3,

    /// <summary>Benzin (litre): firma araçları → Kapsam 1</summary>
    Gasoline = 4,

    /// <summary>LPG (kg): fırın, ısıtma, forklift → Kapsam 1</summary>
    Lpg = 5,

    /// <summary>Kok kömürü (ton): döküm ocakları (kupol) → Kapsam 1</summary>
    Coke = 6,

    /// <summary>Linyit (ton): kazan, ısıtma → Kapsam 1</summary>
    Lignite = 7
}

/// <summary>
/// GHG Protocol kapsamları. Kapsam 3 (tedarik zinciri, ulaşım vb.) bu sürümde hesaplanmaz.
/// </summary>
public enum EmissionScope
{
    /// <summary>Doğrudan emisyon: firmanın kendi yaktığı yakıtlar.</summary>
    Scope1 = 1,

    /// <summary>Dolaylı emisyon: satın alınan elektrik.</summary>
    Scope2 = 2
}
