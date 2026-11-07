using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Tests;

/// <summary>
/// Skor hesaplama kurallarının testleri.
/// Formül: boyut skoru = (ortalama − 1) / 4 × 100, genel skor = 4 boyutun ortalaması.
/// </summary>
public class ScoreCalculatorTests
{
    // ---------------- Boyut skoru ----------------

    [Theory]
    [InlineData(new[] { 1, 1, 1, 1 }, 0)]      // en düşük
    [InlineData(new[] { 3, 3, 3, 3 }, 50)]     // orta
    [InlineData(new[] { 5, 5, 5, 5 }, 100)]    // en yüksek
    [InlineData(new[] { 2, 3, 4, 5 }, 62.5)]   // ortalama 3,5
    [InlineData(new[] { 1, 2, 1, 2 }, 12.5)]   // ortalama 1,5
    [InlineData(new[] { 4, 4, 4, 5 }, 81.25)]  // ortalama 4,25
    public void DimensionScore_OrtalamayiSifirYuzAraliginaCevirir(int[] answers, double expected)
    {
        var score = ScoreCalculator.DimensionScore(answers);

        Assert.Equal((decimal)expected, score);
    }

    [Fact]
    public void DimensionScore_IkiHaneyeYuvarlar()
    {
        // Ortalama 4/3 = 1,333... → (0,333.../4)*100 = 8,333... → 8,33
        var score = ScoreCalculator.DimensionScore(new[] { 1, 1, 2 });

        Assert.Equal(8.33m, score);
    }

    [Fact]
    public void DimensionScore_YanitYoksaSifirDoner()
    {
        Assert.Equal(0m, ScoreCalculator.DimensionScore(Array.Empty<int>()));
    }

    // ---------------- Genel skor ----------------

    [Fact]
    public void TotalScore_DortBoyutunOrtalamasidir()
    {
        // (100 + 50 + 0 + 62,5) / 4 = 53,125 → 53,13 (yarım değer yukarı yuvarlanır)
        var total = ScoreCalculator.TotalScore(new[] { 100m, 50m, 0m, 62.5m });

        Assert.Equal(53.13m, total);
    }

    [Fact]
    public void TotalScore_BosListedeSifirDoner()
    {
        Assert.Equal(0m, ScoreCalculator.TotalScore(Array.Empty<decimal>()));
    }

    // ---------------- Olgunluk seviyesi ----------------

    [Theory]
    [InlineData(0, "Başlangıç")]
    [InlineData(19.99, "Başlangıç")]
    [InlineData(20, "Farkındalık")]     // sınır değer: 20 artık Farkındalık
    [InlineData(39.99, "Farkındalık")]
    [InlineData(40, "Gelişen")]
    [InlineData(59.99, "Gelişen")]
    [InlineData(60, "Olgun")]
    [InlineData(79.99, "Olgun")]
    [InlineData(80, "Öncü")]
    [InlineData(100, "Öncü")]
    public void MaturityLevel_SinirDegerlerdeDogruSeviyeyiVerir(double score, string expected)
    {
        Assert.Equal(expected, ScoreCalculator.MaturityLevel((decimal)score));
    }

    // ---------------- Boyut adları ----------------

    [Theory]
    [InlineData(Dimension.DigitalInfrastructure, "Dijital Altyapı")]
    [InlineData(Dimension.EnergyEfficiency, "Enerji Verimliliği")]
    [InlineData(Dimension.CarbonManagement, "Karbon Yönetimi")]
    [InlineData(Dimension.SupplyChain, "Tedarik Zinciri")]
    public void DisplayName_TurkceAdDoner(Dimension dimension, string expected)
    {
        Assert.Equal(expected, ScoreCalculator.DisplayName(dimension));
    }
}
