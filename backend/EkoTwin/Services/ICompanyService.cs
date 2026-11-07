using EkoTwin.Dtos;
using EkoTwin.Models;

namespace EkoTwin.Services;

public interface ICompanyService
{
    /// <summary>Firmaları listeler (en yeni önce). İsteğe bağlı sektör ve ad filtresi.</summary>
    Task<IReadOnlyList<CompanyResponse>> ListAsync(SectorType? sector = null, string? search = null, CancellationToken ct = default);

    /// <summary>Tek bir firma; yoksa null.</summary>
    Task<CompanyResponse?> GetAsync(Guid id, CancellationToken ct = default);

    Task<CompanyResponse> CreateAsync(CompanyRequest request, CancellationToken ct = default);

    /// <summary>Firmayı günceller; firma yoksa null.</summary>
    Task<CompanyResponse?> UpdateAsync(Guid id, CompanyRequest request, CancellationToken ct = default);

    /// <summary>Firmayı ve tüm değerlendirmelerini siler; firma yoksa false.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
