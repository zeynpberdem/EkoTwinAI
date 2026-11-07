using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Services;

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;

    public CompanyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CompanyResponse>> ListAsync(SectorType? sector = null, string? search = null, CancellationToken ct = default)
    {
        var query = _db.Companies.AsNoTracking();

        if (sector is not null)
            query = query.Where(c => c.Sector == sector);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.CompanyName.ToLower().Contains(term));
        }

        var companies = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return companies.Select(CompanyResponse.From).ToList();
    }

    public async Task<CompanyResponse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return company is null ? null : CompanyResponse.From(company);
    }

    public async Task<CompanyResponse> CreateAsync(CompanyRequest request, CancellationToken ct = default)
    {
        var company = new Company { CreatedAt = DateTime.UtcNow };
        Apply(company, request);

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);

        return CompanyResponse.From(company);
    }

    public async Task<CompanyResponse?> UpdateAsync(Guid id, CompanyRequest request, CancellationToken ct = default)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null)
            return null;

        Apply(company, request); // CreatedAt değişmez
        await _db.SaveChangesAsync(ct);

        return CompanyResponse.From(company);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null)
            return false;

        // Veritabanındaki CASCADE kuralı, firmanın değerlendirmelerini ve yanıtlarını da siler.
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>İstekteki alanları entity'ye aktarır (ekleme ve güncellemede ortak).</summary>
    private static void Apply(Company company, CompanyRequest request)
    {
        company.CompanyName = request.CompanyName.Trim();
        company.Sector = request.Sector!.Value; // [Required] ile doğrulanmış olur
        company.SubSector = request.SubSector?.Trim() ?? string.Empty;
        company.EmployeeCount = request.EmployeeCount;
        company.ExportsToEu = request.ExportsToEu;
        company.CbamProductGroup = request.CbamProductGroup;
    }
}
