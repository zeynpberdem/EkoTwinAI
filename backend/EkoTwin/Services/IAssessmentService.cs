using EkoTwin.Dtos;

namespace EkoTwin.Services;

public interface IAssessmentService
{
    /// <summary>
    /// Yanıtları doğrular, skorları hesaplar ve değerlendirmeyi kaydeder.
    /// Firma yoksa <see cref="KeyNotFoundException"/>,
    /// yanıtlar geçersizse <see cref="AssessmentValidationException"/> fırlatır.
    /// </summary>
    Task<AssessmentResponse> CreateAsync(CreateAssessmentRequest request, CancellationToken ct = default);

    /// <summary>Değerlendirme raporunu getirir; yoksa null.</summary>
    Task<AssessmentResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Firmanın değerlendirmelerini (yeniden eskiye) getirir; firma yoksa null.</summary>
    Task<IReadOnlyList<AssessmentSummaryResponse>?> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
}
