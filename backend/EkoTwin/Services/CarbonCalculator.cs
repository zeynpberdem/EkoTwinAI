using EkoTwin.Models;

namespace EkoTwin.Services;

/// <summary>
/// Kurumsal karbon ayak izi hesabı (GHG Protocol Kapsam 1 + Kapsam 2). Veritabanından bağımsızdır.
///
///   Kaynak emisyonu (ton CO2e) = Miktar × Faktör (kg CO2e/birim) ÷ 1000   → 3 haneye yuvarlanır
///   Kapsam 1 = yakıtların toplamı, Kapsam 2 = elektrik, Toplam = Kapsam 1 + Kapsam 2
///   Toplamlar yuvarlanmış satırlardan toplanır; böylece raporda satırlar toplamla birebir tutar.
/// </summary>
public static class CarbonCalculator
{
    public record Entry(EmissionSource Source, decimal Quantity);

    public record Result(
        IReadOnlyList<CarbonFootprintItem> Items,
        decimal Scope1Tonnes,
        decimal Scope2Tonnes,
        decimal TotalTonnes,
        decimal TonnesPerEmployee);

    /// <param name="entries">Girilen miktarlar (her kaynak en fazla bir kez; sıfır olanlar atlanır).</param>
    /// <param name="factors">Kaynak → emisyon faktörü.</param>
    /// <param name="employeeCount">Çalışan sayısı (0 ise çalışan başına değer 0 olur).</param>
    public static Result Calculate(
        IEnumerable<Entry> entries,
        IReadOnlyDictionary<EmissionSource, EmissionFactor> factors,
        int employeeCount)
    {
        var items = entries
            .Where(e => e.Quantity > 0)
            .OrderBy(e => e.Source)
            .Select(e =>
            {
                var f = factors[e.Source];
                return new CarbonFootprintItem
                {
                    Source = e.Source,
                    Scope = f.Scope,
                    Quantity = e.Quantity,
                    Unit = f.Unit,
                    FactorKgCo2ePerUnit = f.KgCo2ePerUnit,
                    FactorReference = f.Reference,
                    EmissionTonnes = Round3(e.Quantity * f.KgCo2ePerUnit / 1000m)
                };
            })
            .ToList();

        var scope1 = items.Where(i => i.Scope == EmissionScope.Scope1).Sum(i => i.EmissionTonnes);
        var scope2 = items.Where(i => i.Scope == EmissionScope.Scope2).Sum(i => i.EmissionTonnes);
        var total = scope1 + scope2;
        var perEmployee = employeeCount > 0 ? Round3(total / employeeCount) : 0m;

        return new Result(items, scope1, scope2, total, perEmployee);
    }

    /// <summary>Toplam içindeki pay (%), 1 hane. Toplam 0 ise 0.</summary>
    public static decimal SharePercent(decimal part, decimal total) =>
        total == 0 ? 0m : Math.Round(part / total * 100m, 1, MidpointRounding.AwayFromZero);

    /// <summary>Önceki yıla göre değişim (%), 1 hane. Önceki toplam 0 ise null.</summary>
    public static decimal? ChangePercent(decimal previous, decimal current) =>
        previous == 0 ? null : Math.Round((current - previous) / previous * 100m, 1, MidpointRounding.AwayFromZero);

    /// <summary>En büyük emisyon kaynağına göre sade dilde yönlendirme.</summary>
    public static string GuidanceFor(EmissionSource source) => source switch
    {
        EmissionSource.Electricity =>
            "Elektrik tüketimini makine bazında izlemek (anlık sayaç), boşta çalışmayı azaltmak ve çatı güneş enerjisi (GES) fizibilitesi en büyük etkiyi yaratır.",
        EmissionSource.NaturalGas or EmissionSource.Lpg or EmissionSource.Lignite =>
            "Kazan/fırın verimliliği, bina ve boru yalıtımı ile atık ısının geri kazanılması yakıt tüketimini ve emisyonu birlikte düşürür.",
        EmissionSource.Diesel or EmissionSource.Gasoline =>
            "Araç ve forklift kullanımının planlanması, rota optimizasyonu ve jeneratör kullanımının azaltılması öncelikli adımlardır.",
        EmissionSource.Coke =>
            "Ocak şarj ve yanma optimizasyonu öncelikli adımdır; orta vadede elektrikli (indüksiyon) ocağa geçişin fizibilitesi değerlendirilebilir.",
        _ => "Bu kaynağın tüketimini izlemek ve azaltım fırsatlarını belirlemek ilk adımdır."
    };

    private static decimal Round3(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
