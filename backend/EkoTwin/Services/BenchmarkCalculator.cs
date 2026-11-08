using EkoTwin.Dtos;
using EkoTwin.Models;
using static EkoTwin.Services.TurkishFormat;

namespace EkoTwin.Services;

/// <summary>
/// Sektör karşılaştırması ("Benzer firmalara göre neredeyim?"). Veritabanından bağımsızdır.
///
/// GRUP SEÇİMİ (kademeli; firmanın kendisi hariç en az 5 firma gerekir)
///   1) Aynı sektör + aynı büyüklük  2) Aynı sektör  3) Aynı büyüklük  4) Tüm firmalar
///   Hiçbirinde 5 firma yoksa karşılaştırma gösterilmez (yanıltıcı ortalama ve gizlilik riski).
///
/// HESAP (her firmanın yalnızca en son değerlendirmesi / en son karbon kaydı)
///   Grup ortalaması, "daha iyi olduğu firma oranı" (puanı kesin olarak düşük olanlar / grup)
///   ve "10 firmadan kaçından" (oran × 10, yuvarlanmış).
///   Konum: fark ≥ +5 puan → ortalamanın üstü, ≤ −5 → altı, arası → ortalama düzey
///   (5 puan eşiği EkoTwin tasarım kararıdır; tek bir sorudaki 1 puanlık fark boyutta 6,25 puandır).
///   Karbonda düşük olan iyidir: çalışan başına karbonu kesin olarak yüksek olan firmalar sayılır.
/// </summary>
public static class BenchmarkCalculator
{
    public const int MinGroupSize = 5;
    public const decimal PositionThreshold = 5m;
    public const decimal CarbonPositionThresholdPercent = 10m;

    public record ScoreRow(Guid CompanyId, SectorType Sector, SizeClass Size,
        decimal Total, decimal Digital, decimal Energy, decimal Carbon, decimal Supply);

    public record CarbonRow(Guid CompanyId, SectorType Sector, SizeClass Size, int Year, decimal TonnesPerEmployee);

    public static SizeClass SizeOf(int employeeCount) => employeeCount switch
    {
        <= 9 => SizeClass.Micro,
        <= 49 => SizeClass.Small,
        _ => SizeClass.Medium
    };

    public static string SizeClassName(SizeClass size) => size switch
    {
        SizeClass.Micro => "Mikro (1-9 çalışan)",
        SizeClass.Small => "Küçük (10-49 çalışan)",
        SizeClass.Medium => "Orta (50-249 çalışan)",
        _ => size.ToString()
    };

    private static string SizeRange(SizeClass size) => size switch
    {
        SizeClass.Micro => "1-9 çalışan",
        SizeClass.Small => "10-49 çalışan",
        SizeClass.Medium => "50-249 çalışan",
        _ => size.ToString()
    };

    /// <summary>Kademeli grup seçimi. Uygun grup yoksa null.</summary>
    public static (BenchmarkGroupLevel Level, List<T> Peers)? SelectGroup<T>(
        Guid companyId, SectorType sector, SizeClass size, IEnumerable<T> rows,
        Func<T, Guid> idOf, Func<T, SectorType> sectorOf, Func<T, SizeClass> sizeOf)
    {
        var others = rows.Where(r => idOf(r) != companyId).ToList();

        var candidates = new (BenchmarkGroupLevel, Func<T, bool>)[]
        {
            (BenchmarkGroupLevel.SectorAndSize, r => sectorOf(r) == sector && sizeOf(r) == size),
            (BenchmarkGroupLevel.Sector, r => sectorOf(r) == sector),
            (BenchmarkGroupLevel.Size, r => sizeOf(r) == size),
            (BenchmarkGroupLevel.All, _ => true)
        };

        foreach (var (level, filter) in candidates)
        {
            var peers = others.Where(filter).ToList();
            if (peers.Count >= MinGroupSize)
                return (level, peers);
        }

        return null;
    }

    public static string GroupDescription(BenchmarkGroupLevel level, SectorType sector, SizeClass size, int count) => level switch
    {
        BenchmarkGroupLevel.SectorAndSize =>
            $"{Lookups.SectorName(sector)} sektöründeki, sizin büyüklüğünüzdeki ({SizeRange(size)}) {count} firmayla karşılaştırıldınız.",
        BenchmarkGroupLevel.Sector => $"{Lookups.SectorName(sector)} sektöründeki {count} firmayla karşılaştırıldınız.",
        BenchmarkGroupLevel.Size => $"Sizin büyüklüğünüzdeki ({SizeRange(size)}) {count} firmayla karşılaştırıldınız.",
        _ => $"Sistemdeki {count} firmayla karşılaştırıldınız."
    };

    public const string NotEnoughPeers =
        "Karşılaştırma için henüz yeterli firma yok. Benzer en az 5 firma test doldurduğunda burada görünecek.";

    /// <summary>Genel puan ve 4 boyut için karşılaştırma.</summary>
    public static List<BenchmarkMetricResponse> CompareScores(ScoreRow you, IReadOnlyCollection<ScoreRow> peers)
    {
        var metrics = new List<(Dimension? Dim, Func<ScoreRow, decimal> Get)>
        {
            (null, r => r.Total),
            (Dimension.DigitalInfrastructure, r => r.Digital),
            (Dimension.EnergyEfficiency, r => r.Energy),
            (Dimension.CarbonManagement, r => r.Carbon),
            (Dimension.SupplyChain, r => r.Supply)
        };

        return metrics.Select(m =>
        {
            var mine = m.Get(you);
            var values = peers.Select(m.Get).ToList();
            var average = Round(values.Average(), 2);
            var share = Round((decimal)values.Count(v => v < mine) / values.Count * 100m, 1);
            var outOf10 = OutOf10(share);
            var position = PositionOf(mine - average, PositionThreshold);

            var name = m.Dim is { } d ? ScoreCalculator.DisplayName(d) : "Genel puan";
            var simpleName = m.Dim is { } d2 ? ScoreCalculator.SimpleName(d2) : "Genel puan";
            var topic = m.Dim is null ? "Genel puanda" : $"{simpleName} konusunda";

            var text = $"{topic} puanınız {PlainLanguage.Points(mine)}, ortalama {PlainLanguage.Points(average)}. " +
                       $"{PositionText(position)} {BetterThanText(outOf10)}";

            return new BenchmarkMetricResponse(m.Dim, name, simpleName, mine, average, share, outOf10, position, text);
        }).ToList();
    }

    /// <summary>Çalışan başına karbon karşılaştırması (düşük olan iyi).</summary>
    public static CarbonBenchmarkResponse CompareCarbon(CarbonRow you, BenchmarkGroupResponse group, IReadOnlyCollection<CarbonRow> peers)
    {
        var values = peers.Select(p => p.TonnesPerEmployee).ToList();
        var average = Round(values.Average(), 3);
        var share = Round((decimal)values.Count(v => v > you.TonnesPerEmployee) / values.Count * 100m, 1);
        var outOf10 = OutOf10(share);

        // Karbonda konum: ortalamadan %10'dan fazla düşük → iyi (Above), %10'dan fazla yüksek → Below
        var relative = average == 0 ? 0 : (average - you.TonnesPerEmployee) / average * 100m;
        var position = PositionOf(relative, CarbonPositionThresholdPercent);

        var positionText = position switch
        {
            BenchmarkPosition.Above => "Benzer firmalardan daha az karbon salıyorsunuz.",
            BenchmarkPosition.Below => "Benzer firmalardan daha fazla karbon salıyorsunuz.",
            _ => "Benzer firmalarla aynı düzeydesiniz."
        };

        var text = $"Çalışan başına karbonunuz {PlainLanguage.Tonnes(you.TonnesPerEmployee)}, " +
                   $"ortalama {PlainLanguage.Tonnes(average)}. {positionText} {LessThanText(outOf10)}";

        return new CarbonBenchmarkResponse(group, you.Year, you.TonnesPerEmployee, average, share, outOf10, position, text);
    }

    /// <summary>Doğrulanmış Türkiye geneli referanslar; firmanın büyüklüğüne ve cevabına göre.</summary>
    public static List<NationalReferenceResponse> NationalReferences(
        SizeClass size, IEnumerable<BenchmarkReference> references, IReadOnlyDictionary<int, int> answersByQuestion)
    {
        if (size == SizeClass.Micro)
            return new List<NationalReferenceResponse>(); // TÜİK araştırması 10+ çalışanı kapsar

        return references
            .Where(r => r.IsVerified && (r.SizeClass is null || r.SizeClass == size))
            .OrderBy(r => r.QuestionNumber ?? int.MaxValue).ThenBy(r => r.Indicator)
            .Select(r =>
            {
                var who = r.SizeClass is null
                    ? "10 ve üzeri çalışanı olan firmaların"
                    : $"sizin büyüklüğünüzdeki ({SizeRange(size)}) firmaların";
                var text = $"Türkiye'de {who} {PlainLanguage.PercentPossessive(r.Percent)} {r.SimpleIndicator}.";

                bool? included = null;
                string? status = null;
                if (r.QuestionNumber is { } q && r.MinAnswerScore is { } min && answersByQuestion.TryGetValue(q, out var answer))
                {
                    included = answer >= min;
                    status = included.Value ? "Siz de bunlardan birisiniz." : "Siz henüz bunlardan biri değilsiniz.";
                }

                return new NationalReferenceResponse(r.Indicator, r.Percent, r.Year, r.Source, text, included, status);
            })
            .ToList();
    }

    // ------------------------------------------------------------------

    private static int OutOf10(decimal sharePercent) =>
        (int)Math.Round(sharePercent / 10m, 0, MidpointRounding.AwayFromZero);

    private static BenchmarkPosition PositionOf(decimal difference, decimal threshold) =>
        difference >= threshold ? BenchmarkPosition.Above
        : difference <= -threshold ? BenchmarkPosition.Below
        : BenchmarkPosition.Average;

    private static string PositionText(BenchmarkPosition p) => p switch
    {
        BenchmarkPosition.Above => "Ortalamanın üstündesiniz.",
        BenchmarkPosition.Below => "Ortalamanın altındasınız.",
        _ => "Ortalama düzeydesiniz."
    };

    /// <summary>"10 firmadan 7'sinden daha iyisiniz." (ekler sayıya göre elle yazılır)</summary>
    public static string BetterThanText(int outOf10) => outOf10 <= 0
        ? "Şu an benzer firmaların çoğunun gerisindesiniz."
        : $"10 firmadan {WithAblative(outOf10)} daha iyisiniz.";

    public static string LessThanText(int outOf10) => outOf10 <= 0
        ? "Benzer firmaların çoğundan daha fazla karbon salıyorsunuz."
        : $"10 firmadan {WithAblative(outOf10)} daha az karbon salıyorsunuz.";

    /// <summary>1 → "1'inden", 6 → "6'sından", 10 → "10'undan" (Türkçe ünlü uyumuna göre).</summary>
    public static string WithAblative(int n) => n switch
    {
        1 => "1'inden",
        2 => "2'sinden",
        3 => "3'ünden",
        4 => "4'ünden",
        5 => "5'inden",
        6 => "6'sından",
        7 => "7'sinden",
        8 => "8'inden",
        9 => "9'undan",
        10 => "10'undan",
        _ => n.ToString(Culture)
    };

    private static decimal Round(decimal value, int digits) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
}
