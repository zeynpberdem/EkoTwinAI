using EkoTwin.Data;
using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Tests;

/// <summary>Karbon hesap formülü ve emisyon faktörlerinin doğruluğu.</summary>
public class CarbonCalculatorTests
{
    private static readonly IReadOnlyDictionary<EmissionSource, EmissionFactor> Factors =
        EmissionFactorSeed.Factors.ToDictionary(f => f.Source);

    private static CarbonCalculator.Result Calc(int employees, params (EmissionSource s, decimal q)[] entries) =>
        CarbonCalculator.Calculate(entries.Select(e => new CarbonCalculator.Entry(e.s, e.q)), Factors, employees);

    // ===================== Hesap formülü =====================

    [Fact]
    public void Elektrik_100BinKwh_47Virgul8Ton_Kapsam2()
    {
        // 100.000 kWh × 0,478 kg/kWh = 47.800 kg = 47,8 t
        var r = Calc(10, (EmissionSource.Electricity, 100_000m));

        Assert.Equal(47.8m, r.TotalTonnes);
        Assert.Equal(47.8m, r.Scope2Tonnes);
        Assert.Equal(0m, r.Scope1Tonnes);
        Assert.Equal(EmissionScope.Scope2, r.Items.Single().Scope);
    }

    [Fact]
    public void KarisikKaynaklar_KapsamlaraDogruAyrilir_ToplamSatirlarinToplami()
    {
        var r = Calc(40,
            (EmissionSource.Electricity, 250_000m),   // 119,5 t (Kapsam 2)
            (EmissionSource.NaturalGas, 30_000m),     // 30.000 × 1,9372 = 58,116 t
            (EmissionSource.Diesel, 5_000m),          // 5.000 × 2,6853 = 13,4265 → 13,427 t
            (EmissionSource.Coke, 100m));             // 100 × 3.036,5055 = 303,65055 → 303,651 t

        Assert.Equal(119.5m, r.Scope2Tonnes);
        Assert.Equal(58.116m + 13.427m + 303.651m, r.Scope1Tonnes);
        Assert.Equal(r.Scope1Tonnes + r.Scope2Tonnes, r.TotalTonnes);
        Assert.Equal(r.Items.Sum(i => i.EmissionTonnes), r.TotalTonnes);
    }

    [Fact]
    public void SifirMiktar_SatirOlusturmaz()
    {
        var r = Calc(10, (EmissionSource.Electricity, 1000m), (EmissionSource.Lpg, 0m));

        Assert.Single(r.Items);
        Assert.Equal(EmissionSource.Electricity, r.Items[0].Source);
    }

    [Fact]
    public void CalisanBasinaEmisyon_HesaplanirVeCalisanYoksaSifir()
    {
        Assert.Equal(4.78m, Calc(10, (EmissionSource.Electricity, 100_000m)).TonnesPerEmployee);
        Assert.Equal(0m, Calc(0, (EmissionSource.Electricity, 100_000m)).TonnesPerEmployee);
    }

    [Fact]
    public void Satir_KullanilanFaktorVeKaynagiSaklar()
    {
        var item = Calc(10, (EmissionSource.Lpg, 1000m)).Items.Single();

        Assert.Equal(Factors[EmissionSource.Lpg].KgCo2ePerUnit, item.FactorKgCo2ePerUnit);
        Assert.Equal("kg", item.Unit);
        Assert.Contains("IPCC", item.FactorReference);
    }

    [Theory]
    [InlineData(25, 100, 25.0)]
    [InlineData(1, 3, 33.3)]
    [InlineData(5, 0, 0)]
    public void PayYuzdesi(decimal part, decimal total, decimal expected) =>
        Assert.Equal(expected, CarbonCalculator.SharePercent(part, total));

    [Fact]
    public void DegisimYuzdesi_AzalmaNegatif_OncekiSifirsaNull()
    {
        Assert.Equal(-20.0m, CarbonCalculator.ChangePercent(100m, 80m));
        Assert.Equal(12.5m, CarbonCalculator.ChangePercent(80m, 90m));
        Assert.Null(CarbonCalculator.ChangePercent(0m, 50m));
    }

    // ===================== Emisyon faktörleri =====================

    [Fact]
    public void HerKaynakIcin_TamBirFaktorVar()
    {
        var sources = EmissionFactorSeed.Factors.Select(f => f.Source).ToList();

        Assert.Equal(Enum.GetValues<EmissionSource>().OrderBy(s => s), sources.OrderBy(s => s));
        Assert.Equal(sources.Count, sources.Distinct().Count());
    }

    [Fact]
    public void YalnizcaElektrik_Kapsam2()
    {
        foreach (var f in EmissionFactorSeed.Factors)
            Assert.Equal(f.Source == EmissionSource.Electricity ? EmissionScope.Scope2 : EmissionScope.Scope1, f.Scope);
    }

    [Fact]
    public void HerFaktorunKaynagiVeBirimiVar()
    {
        foreach (var f in EmissionFactorSeed.Factors)
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Reference), $"{f.Source}: kaynak eksik");
            Assert.False(string.IsNullOrWhiteSpace(f.Unit), $"{f.Source}: birim eksik");
            Assert.True(f.Reference.Length <= 500, $"{f.Source}: kaynak metni 500 karakteri aşıyor");
            Assert.True(f.KgCo2ePerUnit > 0);
        }
    }

    /// <summary>
    /// IPCC parametrelerinden elle yapılan hesapla karşılaştırma (yazım hatası koruması).
    /// Örn. motorin: 43,0 MJ/kg × 0,84 kg/L = 36,12 MJ/L; (74.100 + 3×28 + 0,6×265) = 74.343 kg/TJ
    ///               → 36,12 × 74.343 / 10^6 = 2,6853 kg CO2e/L
    /// </summary>
    [Theory]
    [InlineData(EmissionSource.Electricity, 0.478)]
    [InlineData(EmissionSource.NaturalGas, 1.9372)]
    [InlineData(EmissionSource.Diesel, 2.6853)]
    [InlineData(EmissionSource.Gasoline, 2.2952)]
    [InlineData(EmissionSource.Lpg, 2.9872)]
    [InlineData(EmissionSource.Coke, 3036.5055)]
    [InlineData(EmissionSource.Lignite, 1209.9623)]
    public void FaktorDegerleri_ElleHesapla_Tutar(EmissionSource source, double expected) =>
        Assert.Equal((decimal)expected, Factors[source].KgCo2ePerUnit);
}
