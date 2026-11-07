using System.Globalization;
using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Services;

public class CarbonFootprintService : ICarbonFootprintService
{
    /// <summary>Raporda her zaman gösterilen yöntem sınırları (sonucun yanlış yorumlanmaması için).</summary>
    public static readonly IReadOnlyList<string> Limitations = new[]
    {
        "Kapsam 3 (satın alınan malzemeler, tedarik zinciri, çalışan ulaşımı vb.) dahil değildir.",
        "Süreç emisyonları (ör. kireçtaşı ayrışması, elektrot tüketimi) ve soğutucu gaz kaçakları dahil değildir.",
        "Elektrik için Türkiye ortalama şebeke faktörü kullanılmıştır; belgeli yenilenebilir enerji alımı ayrıca dikkate alınmamıştır.",
        "Sonuç ön değerlendirme niteliğindedir; resmi raporlama ve beyanlar için akredite doğrulama gerekir."
    };

    public const string CbamNoteText =
        "AB'li müşterileriniz CBAM kapsamında ürün başına gömülü emisyon (ton ürün başına t CO2e) isteyecektir. " +
        "Bu kurumsal hesap o çalışmanın veri temelidir; ürün bazlı değer için üretim miktarları ve süreç emisyonları da gerekir.";

    private static readonly CultureInfo Turkish = TurkishFormat.Culture;

    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public CarbonFootprintService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<EmissionFactorResponse>> GetFactorsAsync(CancellationToken ct = default)
    {
        var factors = await _db.EmissionFactors.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Source)
            .ToListAsync(ct);
        return factors.Select(EmissionFactorResponse.From).ToList();
    }

    public async Task<CarbonFootprintResponse> CreateAsync(Guid companyId, CarbonFootprintRequest request, CancellationToken ct = default)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct)
                      ?? throw new KeyNotFoundException($"Firma bulunamadı: {companyId}");

        var factors = await LoadFactorsAsync(ct);
        Validate(request, factors);

        if (await _db.CarbonFootprints.AnyAsync(c => c.CompanyId == companyId && c.Year == request.Year, ct))
            throw new CarbonYearConflictException(request.Year);

        var footprint = new CarbonFootprint
        {
            CompanyId = companyId,
            Year = request.Year,
            CreatedAt = _clock.GetUtcNow().UtcDateTime
        };
        ApplyCalculation(footprint, request, factors, company.EmployeeCount);

        _db.CarbonFootprints.Add(footprint);
        await _db.SaveChangesAsync(ct);

        return (await GetAsync(footprint.Id, ct))!;
    }

    public async Task<CarbonFootprintResponse?> UpdateAsync(Guid id, CarbonFootprintRequest request, CancellationToken ct = default)
    {
        var footprint = await _db.CarbonFootprints
            .Include(c => c.Company)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (footprint is null)
            return null;

        var factors = await LoadFactorsAsync(ct);
        Validate(request, factors);

        if (request.Year != footprint.Year &&
            await _db.CarbonFootprints.AnyAsync(c => c.CompanyId == footprint.CompanyId && c.Year == request.Year && c.Id != id, ct))
            throw new CarbonYearConflictException(request.Year);

        // Eski satırlar silinir, hesap güncel faktörler ve güncel çalışan sayısıyla yeniden yapılır.
        _db.CarbonFootprintItems.RemoveRange(footprint.Items);
        footprint.Items = new List<CarbonFootprintItem>();
        footprint.Year = request.Year;
        footprint.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
        ApplyCalculation(footprint, request, factors, footprint.Company.EmployeeCount);

        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<CarbonFootprintResponse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var footprint = await _db.CarbonFootprints.AsNoTracking()
            .Include(c => c.Company)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (footprint is null)
            return null;

        // Karşılaştırma: aynı firmanın bu yıldan önceki en yakın yılı (okuma anında bulunur;
        // böylece geçmiş yıl sonradan girilse de karşılaştırma görünür).
        var previous = await _db.CarbonFootprints.AsNoTracking()
            .Where(c => c.CompanyId == footprint.CompanyId && c.Year < footprint.Year)
            .OrderByDescending(c => c.Year)
            .Select(c => new { c.Year, c.TotalTonnes })
            .FirstOrDefaultAsync(ct);

        CarbonComparisonResponse? comparison = null;
        if (previous is not null)
        {
            var change = footprint.TotalTonnes - previous.TotalTonnes;
            var percent = CarbonCalculator.ChangePercent(previous.TotalTonnes, footprint.TotalTonnes);
            comparison = new CarbonComparisonResponse(previous.Year, previous.TotalTonnes, change, percent,
                ComparisonSummary(previous.Year, change, percent));
        }

        return ToResponse(footprint, comparison);
    }

    public async Task<IReadOnlyList<CarbonFootprintSummaryResponse>?> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, ct))
            return null;

        return await _db.CarbonFootprints.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.Year)
            .Select(c => new CarbonFootprintSummaryResponse(
                c.Id, c.Year, c.Scope1Tonnes, c.Scope2Tonnes, c.TotalTonnes, c.TonnesPerEmployee))
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var footprint = await _db.CarbonFootprints.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (footprint is null)
            return false;

        _db.CarbonFootprints.Remove(footprint); // satırlar CASCADE ile silinir
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ------------------------------------------------------------------

    private async Task<Dictionary<EmissionSource, EmissionFactor>> LoadFactorsAsync(CancellationToken ct) =>
        await _db.EmissionFactors.AsNoTracking().Where(f => f.IsActive).ToDictionaryAsync(f => f.Source, ct);

    private void Validate(CarbonFootprintRequest request, IReadOnlyDictionary<EmissionSource, EmissionFactor> factors)
    {
        var errors = new List<string>();

        var currentYear = _clock.GetUtcNow().Year;
        if (request.Year > currentYear)
            errors.Add($"Gelecek yıl için hesap yapılamaz (en fazla {currentYear}).");

        var duplicates = request.Entries
            .Where(e => e.Source is not null)
            .GroupBy(e => e.Source!.Value)
            .Where(g => g.Count() > 1)
            .Select(g => Lookups.EmissionSourceName(g.Key))
            .ToList();
        if (duplicates.Count > 0)
            errors.Add($"Aynı kaynak birden fazla kez girilmiş: {string.Join(", ", duplicates)}.");

        var unknown = request.Entries
            .Where(e => e.Source is not null && !factors.ContainsKey(e.Source.Value))
            .Select(e => e.Source!.Value.ToString())
            .Distinct()
            .ToList();
        if (unknown.Count > 0)
            errors.Add($"Bu kaynaklar için emisyon faktörü tanımlı değil: {string.Join(", ", unknown)}.");

        if (request.Entries.Any(e => e.Quantity < 0))
            errors.Add("Miktarlar negatif olamaz.");

        if (request.Entries.All(e => e.Quantity <= 0))
            errors.Add("En az bir kaynak için sıfırdan büyük bir değer girilmelidir.");

        if (errors.Count > 0)
            throw new CarbonValidationException(errors);
    }

    private static void ApplyCalculation(CarbonFootprint footprint, CarbonFootprintRequest request,
        IReadOnlyDictionary<EmissionSource, EmissionFactor> factors, int employeeCount)
    {
        var result = CarbonCalculator.Calculate(
            request.Entries.Select(e => new CarbonCalculator.Entry(e.Source!.Value, e.Quantity)),
            factors,
            employeeCount);

        footprint.Scope1Tonnes = result.Scope1Tonnes;
        footprint.Scope2Tonnes = result.Scope2Tonnes;
        footprint.TotalTonnes = result.TotalTonnes;
        footprint.EmployeeCount = employeeCount;
        footprint.TonnesPerEmployee = result.TonnesPerEmployee;
        foreach (var item in result.Items)
            footprint.Items.Add(item);
    }

    private static string ComparisonSummary(int previousYear, decimal change, decimal? percent)
    {
        if (change == 0)
            return $"{previousYear} yılına göre değişiklik yok.";

        var direction = change < 0 ? "azaldı" : "arttı";
        var amount = string.Format(Turkish, "{0:0.###} t CO2e", Math.Abs(change));
        return percent is null
            ? $"{previousYear} yılına göre {amount} {direction}."
            : string.Format(Turkish, "{0} yılına göre %{1:0.#} ({2}) {3}.", previousYear, Math.Abs(percent.Value), amount, direction);
    }

    private static CarbonFootprintResponse ToResponse(CarbonFootprint f, CarbonComparisonResponse? comparison)
    {
        var items = f.Items
            .OrderByDescending(i => i.EmissionTonnes)
            .ThenBy(i => i.Source)
            .Select(i => new CarbonFootprintItemResponse(
                i.Source, Lookups.EmissionSourceName(i.Source), i.Scope, Lookups.EmissionScopeName(i.Scope),
                i.Quantity, i.Unit, i.FactorKgCo2ePerUnit, i.EmissionTonnes,
                CarbonCalculator.SharePercent(i.EmissionTonnes, f.TotalTonnes), i.FactorReference))
            .ToList();

        var largest = items.FirstOrDefault(i => i.EmissionTonnes > 0);
        var cbamInScope = f.Company.ExportsToEu && f.Company.CbamProductGroup != CbamProductGroup.None;

        return new CarbonFootprintResponse(
            f.Id, f.CompanyId, f.Company.CompanyName, f.Year, f.CreatedAt, f.UpdatedAt,
            f.Scope1Tonnes, f.Scope2Tonnes, f.TotalTonnes, f.EmployeeCount, f.TonnesPerEmployee,
            largest?.Source, largest?.SourceName, largest?.SharePercent ?? 0m,
            largest is null ? null : CarbonCalculator.GuidanceFor(largest.Source),
            comparison,
            cbamInScope ? CbamNoteText : null,
            Limitations,
            items);
    }
}
