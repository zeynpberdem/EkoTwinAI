using EkoTwin.Models;

namespace EkoTwin.Data;

/// <summary>
/// Emisyon faktörleri ve nasıl türetildikleri. Her değer açık bir kaynağa dayanır; rakamlar
/// elle yazılmak yerine kaynak parametrelerinden hesaplanır (denetlenebilirlik için).
///
/// ELEKTRİK (Kapsam 2)
///   T.C. Enerji ve Tabii Kaynaklar Bakanlığı, "2022 Yılı Türkiye Elektrik Üretim ve Tüketim Noktası
///   Emisyon Faktörleri": dağıtım hattına bağlı tüketim noktası = 0,478 tCO2e/MWh (= kg CO2e/kWh).
///   KOBİ'lerin büyük çoğunluğu dağıtım şebekesinden beslendiği için bu değer seçildi.
///
/// YAKITLAR (Kapsam 1)
///   IPCC 2006 Ulusal Sera Gazı Envanteri Rehberi, Cilt 2 (Enerji):
///   - Bölüm 1, Tablo 1.2: alt ısıl değerler (ALD, net calorific value)
///   - Bölüm 2, Tablo 2.3: imalat sanayi sabit yanma varsayılan faktörleri (kg/TJ; CO2, CH4, N2O)
///   Küresel ısınma potansiyelleri: IPCC AR5 (CH4 = 28, N2O = 265).
///   Formül: kg CO2e / birim = ALD (MJ/birim) × (CO2 + CH4×28 + N2O×265) (kg/TJ) ÷ 1.000.000
///
///   Doğal gaz hacmi: EPDK/BOTAŞ referans üst ısıl değeri 9.155 kcal/Sm³; IPCC'ye göre doğal gazda
///   ALD ≈ ÜID × 0,90 → 8.239,5 kcal/Sm³ ≈ 34,50 MJ/Sm³.
///   Sıvı yakıt yoğunlukları (varsayım): motorin 0,84 kg/L, benzin 0,745 kg/L.
///
/// Sınırlar: Türk linyitinin ısıl değeri ocağa göre çok değişir; tedarikçinin analiz raporundaki
/// değer daha doğru sonuç verir. Süreç emisyonları ve soğutucu gaz kaçakları dahil değildir.
/// </summary>
public static class EmissionFactorSeed
{
    public const decimal GwpCh4 = 28m;   // IPCC AR5
    public const decimal GwpN2O = 265m;  // IPCC AR5

    public const decimal KcalToMj = 0.0041868m;

    private static Guid Id(int n) => Guid.Parse($"00000000-0000-7000-8002-{n:D12}");

    /// <summary>Yakıt faktörü: ALD (MJ/birim) ve IPCC kg/TJ değerlerinden kg CO2e/birim.</summary>
    public static decimal FuelFactor(decimal ncvMjPerUnit, decimal co2KgPerTj, decimal ch4KgPerTj, decimal n2oKgPerTj) =>
        Math.Round(ncvMjPerUnit * (co2KgPerTj + ch4KgPerTj * GwpCh4 + n2oKgPerTj * GwpN2O) / 1_000_000m,
            4, MidpointRounding.AwayFromZero);

    // ---- Alt ısıl değerler (MJ / girdi birimi) ----
    public const decimal NaturalGasNcvMjPerSm3 = 9155m * 0.90m * KcalToMj; // ≈ 34,4973
    public const decimal DieselNcvMjPerLitre = 43.0m * 0.84m;              // 36,12
    public const decimal GasolineNcvMjPerLitre = 44.3m * 0.745m;           // 33,0035
    public const decimal LpgNcvMjPerKg = 47.3m;
    public const decimal CokeNcvMjPerTonne = 28.2m * 1000m;                // kok fırını koku
    public const decimal LigniteNcvMjPerTonne = 11.9m * 1000m;

    private const string IpccRef =
        "IPCC 2006 Rehberi Cilt 2: Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma); GWP: IPCC AR5 (CH4 28, N2O 265).";

    public static readonly EmissionFactor[] Factors =
    {
        new()
        {
            Id = Id(1), Source = EmissionSource.Electricity, Scope = EmissionScope.Scope2, Unit = "kWh",
            KgCo2ePerUnit = 0.478m, ReferenceYear = 2022,
            Reference = "T.C. Enerji ve Tabii Kaynaklar Bakanlığı, 2022 Yılı Elektrik Üretim ve Tüketim Noktası Emisyon Faktörleri: dağıtım hattına bağlı tüketim noktası 0,478 tCO2e/MWh."
        },
        new()
        {
            Id = Id(2), Source = EmissionSource.NaturalGas, Scope = EmissionScope.Scope1, Unit = "m³",
            KgCo2ePerUnit = FuelFactor(NaturalGasNcvMjPerSm3, 56_100m, 1m, 0.1m), ReferenceYear = 2006,
            Reference = IpccRef + " Doğal gaz: 9.155 kcal/Sm³ üst ısıl değer (EPDK/BOTAŞ referansı), ALD = ÜID × 0,90."
        },
        new()
        {
            Id = Id(3), Source = EmissionSource.Diesel, Scope = EmissionScope.Scope1, Unit = "litre",
            KgCo2ePerUnit = FuelFactor(DieselNcvMjPerLitre, 74_100m, 3m, 0.6m), ReferenceYear = 2006,
            Reference = IpccRef + " Motorin (gas/diesel oil): ALD 43,0 MJ/kg, yoğunluk varsayımı 0,84 kg/L."
        },
        new()
        {
            Id = Id(4), Source = EmissionSource.Gasoline, Scope = EmissionScope.Scope1, Unit = "litre",
            KgCo2ePerUnit = FuelFactor(GasolineNcvMjPerLitre, 69_300m, 3m, 0.6m), ReferenceYear = 2006,
            Reference = IpccRef + " Benzin (motor gasoline): ALD 44,3 MJ/kg, yoğunluk varsayımı 0,745 kg/L."
        },
        new()
        {
            Id = Id(5), Source = EmissionSource.Lpg, Scope = EmissionScope.Scope1, Unit = "kg",
            KgCo2ePerUnit = FuelFactor(LpgNcvMjPerKg, 63_100m, 1m, 0.1m), ReferenceYear = 2006,
            Reference = IpccRef + " LPG: ALD 47,3 MJ/kg."
        },
        new()
        {
            Id = Id(6), Source = EmissionSource.Coke, Scope = EmissionScope.Scope1, Unit = "ton",
            KgCo2ePerUnit = FuelFactor(CokeNcvMjPerTonne, 107_000m, 10m, 1.5m), ReferenceYear = 2006,
            Reference = IpccRef + " Kok (coke oven coke): ALD 28,2 MJ/kg."
        },
        new()
        {
            Id = Id(7), Source = EmissionSource.Lignite, Scope = EmissionScope.Scope1, Unit = "ton",
            KgCo2ePerUnit = FuelFactor(LigniteNcvMjPerTonne, 101_000m, 10m, 1.5m), ReferenceYear = 2006,
            Reference = IpccRef + " Linyit: IPCC varsayılan ALD 11,9 MJ/kg; Türk linyitlerinde ısıl değer ocağa göre değişir."
        }
    };
}
