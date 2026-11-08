using System.ComponentModel.DataAnnotations;
using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Dtos;

/// <summary>Karbon ayak izi hesabı için istemciden gelen veri (yıllık fatura değerleri).</summary>
public class CarbonFootprintRequest
{
    /// <summary>Verilerin ait olduğu yıl (gelecek yıl olamaz).</summary>
    [Range(2000, 2100, ErrorMessage = "Yıl 2000 ile 2100 arasında olmalıdır.")]
    public int Year { get; set; }

    /// <summary>Kaynak bazında yıllık tüketim. Kullanılmayan kaynaklar gönderilmeyebilir.</summary>
    [Required(ErrorMessage = "En az bir tüketim değeri girilmelidir.")]
    [MinLength(1, ErrorMessage = "En az bir tüketim değeri girilmelidir.")]
    public List<CarbonEntryRequest> Entries { get; set; } = new();
}

public class CarbonEntryRequest
{
    /// <summary>Kaynak (bkz. GET /api/emission-factors).</summary>
    [Required(ErrorMessage = "Kaynak seçilmelidir.")]
    [EnumDataType(typeof(EmissionSource), ErrorMessage = "Geçersiz kaynak.")]
    public EmissionSource? Source { get; set; }

    /// <summary>Yıllık miktar (birimi kaynağa göre: kWh, m³, litre, kg, ton).</summary>
    [Range(typeof(decimal), "0", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Miktar 0 ile 1.000.000.000 arasında olmalıdır.")]
    public decimal Quantity { get; set; }
}

/// <summary>Emisyon faktörü (arayüzde birim ve açıklama göstermek için).</summary>
public record EmissionFactorResponse(
    EmissionSource Source,
    string SourceName,
    EmissionScope Scope,
    string ScopeName,
    string Unit,
    decimal KgCo2ePerUnit,
    int ReferenceYear,
    string Reference)
{
    public static EmissionFactorResponse From(EmissionFactor f) => new(
        f.Source, Lookups.EmissionSourceName(f.Source), f.Scope, Lookups.EmissionScopeName(f.Scope),
        f.Unit, f.KgCo2ePerUnit, f.ReferenceYear, f.Reference);
}

public record CarbonFootprintItemResponse(
    EmissionSource Source,
    string SourceName,
    EmissionScope Scope,
    string ScopeName,
    string SimpleScopeName,
    decimal Quantity,
    string Unit,
    decimal FactorKgCo2ePerUnit,
    decimal EmissionTonnes,
    string SimpleTonnes,
    decimal SharePercent,
    string FactorReference);

/// <summary>
/// Karbon raporunun KOBİ sahibine gösterilen sade özeti: yuvarlanmış rakamlar, teknik terim yok.
/// Kesin değerler ve teknik ifadeler raporun diğer alanlarında (danışman / PDF) durur.
/// </summary>
public record CarbonSimpleResponse(
    string Headline,
    string OwnFuels,
    string Electricity,
    string? PerEmployee,
    string? LargestSource,
    string? Guidance,
    string? Comparison,
    string? CbamNote,
    string Limitation);

/// <summary>Önceki yılın kaydıyla karşılaştırma.</summary>
public record CarbonComparisonResponse(
    int PreviousYear,
    decimal PreviousTotalTonnes,
    decimal ChangeTonnes,
    decimal? ChangePercent,
    string Summary);

/// <summary>
/// Karbon ayak izi raporu. "Karbon salınımım ne?" sorusunun cevabı:
/// toplam, kapsamlar, en büyük kaynak, çalışan başına değer, önceki yılla karşılaştırma ve CBAM notu.
/// </summary>
public record CarbonFootprintResponse(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    int Year,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    decimal Scope1Tonnes,
    decimal Scope2Tonnes,
    decimal TotalTonnes,
    int EmployeeCount,
    decimal TonnesPerEmployee,
    EmissionSource? LargestSource,
    string? LargestSourceName,
    decimal LargestSourceSharePercent,
    string? Guidance,
    CarbonComparisonResponse? Comparison,
    string? CbamNote,
    IReadOnlyList<string> Limitations,
    CarbonSimpleResponse Simple,
    IReadOnlyList<CarbonFootprintItemResponse> Items);

/// <summary>Firmanın yıllık karbon kayıtları listesi (gelişim grafiği için).</summary>
public record CarbonFootprintSummaryResponse(
    Guid Id,
    int Year,
    decimal Scope1Tonnes,
    decimal Scope2Tonnes,
    decimal TotalTonnes,
    decimal TonnesPerEmployee);
