using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Services.Narrative;

public interface INarrativeService
{
    /// <summary>Değerlendirme için yeni mektup üretir ve saklar; değerlendirme yoksa null.</summary>
    Task<NarrativeResponse?> GenerateAsync(Guid assessmentId, CancellationToken ct = default);

    /// <summary>En son üretilmiş mektup; yoksa null.</summary>
    Task<NarrativeResponse?> GetLatestAsync(Guid assessmentId, CancellationToken ct = default);
}

/// <summary>
/// Rapor mektubu akışı:
///   1) Kuralların sonuçlarından bilgi kartı hazırlanır (firma adı olmadan)
///   2) Yapay zekâ ayarlıysa metin istenir; metin temizlenir ve otomatik kontrolden geçirilir
///   3) Ayarlı değilse, hata verirse veya kontrolden geçemezse kural tabanlı şablon kullanılır (neden kaydedilir)
///   4) Firma adıyla hitap eklenir, mektup saklanır
/// </summary>
public class NarrativeService : INarrativeService
{
    public const string TemplateProvider = "template";

    private readonly AppDbContext _db;
    private readonly IAssessmentService _assessments;
    private readonly ICarbonFootprintService _carbon;
    private readonly IProgressService _progress;
    private readonly IBenchmarkService _benchmark;
    private readonly INarrativeWriter _writer;
    private readonly TimeProvider _clock;
    private readonly ILogger<NarrativeService> _logger;

    public NarrativeService(AppDbContext db, IAssessmentService assessments, ICarbonFootprintService carbon,
        IProgressService progress, IBenchmarkService benchmark, INarrativeWriter writer, TimeProvider clock,
        ILogger<NarrativeService> logger)
    {
        _db = db;
        _assessments = assessments;
        _carbon = carbon;
        _progress = progress;
        _benchmark = benchmark;
        _writer = writer;
        _clock = clock;
        _logger = logger;
    }

    public async Task<NarrativeResponse?> GenerateAsync(Guid assessmentId, CancellationToken ct = default)
    {
        var report = await _assessments.GetByIdAsync(assessmentId, ct);
        if (report is null)
            return null;

        var input = new NarrativeInput(
            report,
            await LatestCarbonAsync(report.CompanyId, ct),
            await _progress.GetProgressAsync(report.CompanyId, ct),
            await _benchmark.GetAsync(report.CompanyId, ct));
        var facts = NarrativeFacts.Build(input);

        string? body = null;
        string? fallbackReason;

        if (!_writer.IsConfigured)
        {
            fallbackReason = "Yapay zekâ ayarlı değil (API anahtarı veya model tanımlı değil).";
        }
        else
        {
            try
            {
                var raw = await _writer.WriteAsync(NarrativePrompt.System, NarrativePrompt.User(facts), ct);
                var cleaned = NarrativeValidator.Clean(raw);
                var check = NarrativeValidator.Validate(cleaned, facts);
                if (check.IsValid)
                {
                    body = cleaned;
                    fallbackReason = null;
                }
                else
                {
                    fallbackReason = "Kontrolden geçmedi: " + string.Join(" ", check.Errors);
                }
            }
            catch (NarrativeProviderException ex)
            {
                _logger.LogWarning(ex, "Yapay zekâ metni üretilemedi; şablon kullanılıyor.");
                fallbackReason = ex.Message;
            }
        }

        var source = body is null ? NarrativeSource.Template : NarrativeSource.Ai;
        body ??= NarrativeTemplate.Build(input);

        if (fallbackReason is not null && fallbackReason.Length > 1000)
            fallbackReason = fallbackReason[..1000];

        var narrative = new AssessmentNarrative
        {
            AssessmentId = assessmentId,
            // Firma adı yapay zekâya gönderilmez; hitap burada eklenir.
            Text = $"Sayın {report.CompanyName} yetkilisi,\n\n{body}",
            Source = source,
            Provider = source == NarrativeSource.Ai ? _writer.ProviderName : TemplateProvider,
            Model = _writer.IsConfigured ? _writer.ModelName : null,
            PromptVersion = NarrativePrompt.Version,
            FallbackReason = fallbackReason,
            CreatedAt = _clock.GetUtcNow().UtcDateTime
        };

        _db.AssessmentNarratives.Add(narrative);
        await _db.SaveChangesAsync(ct);
        return NarrativeResponse.From(narrative);
    }

    public async Task<NarrativeResponse?> GetLatestAsync(Guid assessmentId, CancellationToken ct = default)
    {
        var narrative = await _db.AssessmentNarratives.AsNoTracking()
            .Where(n => n.AssessmentId == assessmentId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id) // aynı anda üretildiyse: uuidv7 kimlikler zaman sıralıdır
            .FirstOrDefaultAsync(ct);
        return narrative is null ? null : NarrativeResponse.From(narrative);
    }

    private async Task<CarbonFootprintResponse?> LatestCarbonAsync(Guid companyId, CancellationToken ct)
    {
        var id = await _db.CarbonFootprints.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.Year)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);
        return id is null ? null : await _carbon.GetAsync(id.Value, ct);
    }
}
