using EkoTwin.Dtos;

namespace EkoTwin.Services;

public interface IProgressService
{
    /// <summary>Firmanın gelişim özeti; firma yoksa null.</summary>
    Task<ProgressResponse?> GetProgressAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// İki değerlendirmeyi karşılaştırır; biri yoksa null.
    /// Aynı değerlendirme veya farklı firmalar ise <see cref="ProgressValidationException"/>.
    /// </summary>
    Task<AssessmentComparisonResponse?> CompareAsync(Guid assessmentId, Guid otherAssessmentId, CancellationToken ct = default);

    /// <summary>Firmanın karbon hedefi ve ilerlemesi; hedef yoksa null. Firma yoksa <see cref="KeyNotFoundException"/>.</summary>
    Task<CarbonTargetResponse?> GetTargetAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>Hedefi oluşturur veya günceller. Firma yoksa <see cref="KeyNotFoundException"/>, kural ihlalinde <see cref="ProgressValidationException"/>.</summary>
    Task<CarbonTargetResponse> SetTargetAsync(Guid companyId, CarbonTargetRequest request, CancellationToken ct = default);

    /// <summary>Hedefi siler; yoksa false.</summary>
    Task<bool> DeleteTargetAsync(Guid companyId, CancellationToken ct = default);
}

/// <summary>Karşılaştırma veya hedef girdisi iş kurallarına uymadığında (400).</summary>
public class ProgressValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ProgressValidationException(IReadOnlyList<string> errors) : base(string.Join(" ", errors))
    {
        Errors = errors;
    }
}
