using EkoTwin.Models;

namespace EkoTwin.Services;

/// <summary>
/// 1-5 ölçeğindeki yanıtları 0-100 skora çeviren hesaplama kuralları.
/// Veritabanından bağımsızdır; kolayca test edilebilir ve değiştirilebilir.
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// Bir boyutun skoru: yanıtların ortalaması alınır ve 0-100'e ölçeklenir.
    /// Ortalama 1 → 0 puan, ortalama 3 → 50 puan, ortalama 5 → 100 puan.
    /// Formül: (ortalama − 1) / 4 × 100
    /// </summary>
    public static decimal DimensionScore(IEnumerable<int> answers)
    {
        var list = answers.ToList();
        if (list.Count == 0)
            return 0m;

        var average = (decimal)list.Average();
        return Round((average - 1m) / 4m * 100m);
    }

    /// <summary>Genel skor: 4 boyut skorunun eşit ağırlıklı ortalaması.</summary>
    public static decimal TotalScore(IEnumerable<decimal> dimensionScores)
    {
        var list = dimensionScores.ToList();
        return list.Count == 0 ? 0m : Round(list.Average());
    }

    /// <summary>0-100 skoru 5 kademeli olgunluk seviyesine çevirir.</summary>
    public static string MaturityLevel(decimal score) => score switch
    {
        < 20m => "Başlangıç",
        < 40m => "Farkındalık",
        < 60m => "Gelişen",
        < 80m => "Olgun",
        _ => "Öncü"
    };

    /// <summary>Boyutun Türkçe adı (raporlarda kullanmak için).</summary>
    public static string DisplayName(Dimension dimension) => dimension switch
    {
        Dimension.DigitalInfrastructure => "Dijital Altyapı",
        Dimension.EnergyEfficiency => "Enerji Verimliliği",
        Dimension.CarbonManagement => "Karbon Yönetimi",
        Dimension.SupplyChain => "Tedarik Zinciri",
        _ => dimension.ToString()
    };

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
