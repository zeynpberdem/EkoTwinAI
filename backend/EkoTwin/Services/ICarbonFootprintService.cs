using EkoTwin.Dtos;

namespace EkoTwin.Services;

public interface ICarbonFootprintService
{
    /// <summary>Aktif emisyon faktörleri (arayüzdeki giriş formu için).</summary>
    Task<IReadOnlyList<EmissionFactorResponse>> GetFactorsAsync(CancellationToken ct = default);

    /// <summary>
    /// Yeni yıllık hesap. Firma yoksa <see cref="KeyNotFoundException"/>, girdi hatalıysa
    /// <see cref="CarbonValidationException"/>, yıl zaten varsa <see cref="CarbonYearConflictException"/>.
    /// </summary>
    Task<CarbonFootprintResponse> CreateAsync(Guid companyId, CarbonFootprintRequest request, CancellationToken ct = default);

    /// <summary>Hesabı yeni değerlerle yeniden yapar; kayıt yoksa null.</summary>
    Task<CarbonFootprintResponse?> UpdateAsync(Guid id, CarbonFootprintRequest request, CancellationToken ct = default);

    /// <summary>Tek bir hesap raporu; yoksa null.</summary>
    Task<CarbonFootprintResponse?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Firmanın yıllık kayıtları (yeni yıl önce); firma yoksa null.</summary>
    Task<IReadOnlyList<CarbonFootprintSummaryResponse>?> ListByCompanyAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>Hesabı siler; yoksa false.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
