using EkoTwin.Models;

namespace EkoTwin.Dtos;

/// <summary>Karşılaştırma grubunun seviyesi (en dardan en genişe).</summary>
public enum BenchmarkGroupLevel
{
    SectorAndSize = 1,
    Sector = 2,
    Size = 3,
    All = 4
}

public enum BenchmarkPosition
{
    Below = 1,
    Average = 2,
    Above = 3
}

/// <summary>Firmanın karşılaştırıldığı grup. Firmanın kendisi sayıya dahil değildir.</summary>
public record BenchmarkGroupResponse(
    BenchmarkGroupLevel Level,
    int PeerCount,
    string SimpleDescription);

/// <summary>Bir puan (genel veya boyut) için karşılaştırma. Grup ortalaması yalnızca yeterli firma varsa döner.</summary>
public record BenchmarkMetricResponse(
    Dimension? Dimension,
    string Name,
    string SimpleName,
    decimal YourScore,
    decimal GroupAverage,
    decimal BetterThanPercent,
    int BetterThanOutOf10,
    BenchmarkPosition Position,
    string SimpleText);

/// <summary>Çalışan başına karbon karşılaştırması (düşük olan iyidir).</summary>
public record CarbonBenchmarkResponse(
    BenchmarkGroupResponse Group,
    int Year,
    decimal YourTonnesPerEmployee,
    decimal GroupAverageTonnesPerEmployee,
    decimal LowerThanPercent,
    int LowerThanOutOf10,
    BenchmarkPosition Position,
    string SimpleText);

/// <summary>Türkiye geneli resmi referans (yalnızca doğrulanmış değerler).</summary>
public record NationalReferenceResponse(
    string Indicator,
    decimal Percent,
    int Year,
    string Source,
    string SimpleText,
    bool? YouAreIncluded,
    string? SimpleYourStatus);

/// <summary>
/// "Benzer firmalara göre neredeyim?" Sistem içi karşılaştırma (asıl) + Türkiye geneli resmi referanslar (ek bilgi).
/// Gizlilik: hiçbir firmanın adı veya tek tek puanı dönmez; en az 5 firmalık gruplarla çalışılır.
/// </summary>
public record BenchmarkResponse(
    Guid CompanyId,
    SizeClass SizeClass,
    string SizeClassName,
    bool HasAssessment,
    BenchmarkGroupResponse? Group,
    IReadOnlyList<BenchmarkMetricResponse> Metrics,
    CarbonBenchmarkResponse? Carbon,
    IReadOnlyList<NationalReferenceResponse> NationalReferences,
    IReadOnlyList<string> SimpleHighlights);
