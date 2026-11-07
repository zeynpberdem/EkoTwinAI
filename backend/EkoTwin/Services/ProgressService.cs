using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Services;

public class ProgressService : IProgressService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public ProgressService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ProgressResponse?> GetProgressAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null)
            return null;

        // Zaman çizelgesi için yalnızca skorlar; karşılaştırma için son iki değerlendirmenin tamamı yüklenir.
        var assessments = await _db.Assessments.AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        AssessmentComparisonResponse? comparison = null;
        if (assessments.Count >= 2)
        {
            var lastTwo = assessments.TakeLast(2).Select(a => a.Id).ToList();
            var full = await LoadFullAsync(lastTwo, ct);
            comparison = ProgressAnalyzer.Compare(full[0], full[1]);
        }

        int? daysSince = assessments.Count == 0
            ? null
            : (int)(_clock.GetUtcNow().UtcDateTime - assessments[^1].CreatedAt).TotalDays;

        var carbon = await BuildCarbonTrendAsync(companyId, ct);

        return new ProgressResponse(
            company.Id,
            company.CompanyName,
            assessments.Count,
            assessments.Select(AssessmentSummaryResponse.From).ToList(),
            comparison,
            daysSince,
            daysSince is >= ProgressAnalyzer.ReassessmentIntervalDays,
            carbon,
            ProgressAnalyzer.Highlights(assessments.Count, comparison, daysSince, carbon));
    }

    public async Task<AssessmentComparisonResponse?> CompareAsync(Guid assessmentId, Guid otherAssessmentId, CancellationToken ct = default)
    {
        if (assessmentId == otherAssessmentId)
            throw new ProgressValidationException(new[] { "Bir değerlendirme kendisiyle karşılaştırılamaz." });

        var full = await LoadFullAsync(new[] { assessmentId, otherAssessmentId }, ct);
        if (full.Count < 2)
            return null;

        if (full[0].CompanyId != full[1].CompanyId)
            throw new ProgressValidationException(new[] { "Yalnızca aynı firmanın değerlendirmeleri karşılaştırılabilir." });

        return ProgressAnalyzer.Compare(full[0], full[1]);
    }

    public async Task<CarbonTargetResponse?> GetTargetAsync(Guid companyId, CancellationToken ct = default)
    {
        await EnsureCompanyAsync(companyId, ct);

        var target = await _db.CarbonTargets.AsNoTracking().FirstOrDefaultAsync(t => t.CompanyId == companyId, ct);
        return target is null ? null : await EvaluateAsync(target, ct);
    }

    public async Task<CarbonTargetResponse> SetTargetAsync(Guid companyId, CarbonTargetRequest request, CancellationToken ct = default)
    {
        await EnsureCompanyAsync(companyId, ct);

        var errors = new List<string>();
        if (request.TargetYear <= request.BaseYear)
            errors.Add("Hedef yıl, baz yıldan sonra olmalıdır.");
        if (request.ReductionPercent is <= 0 or > 100)
            errors.Add("Azaltma oranı 0'dan büyük ve en fazla 100 olmalıdır.");
        if (!await _db.CarbonFootprints.AnyAsync(c => c.CompanyId == companyId && c.Year == request.BaseYear, ct))
            errors.Add($"Önce baz yıl ({request.BaseYear}) için karbon hesabı yapılmalıdır.");
        if (errors.Count > 0)
            throw new ProgressValidationException(errors);

        var now = _clock.GetUtcNow().UtcDateTime;
        var target = await _db.CarbonTargets.FirstOrDefaultAsync(t => t.CompanyId == companyId, ct);
        if (target is null)
        {
            target = new CarbonTarget { CompanyId = companyId, CreatedAt = now };
            _db.CarbonTargets.Add(target);
        }
        else
        {
            target.UpdatedAt = now;
        }

        target.BaseYear = request.BaseYear;
        target.TargetYear = request.TargetYear;
        target.ReductionPercent = request.ReductionPercent;

        await _db.SaveChangesAsync(ct);
        return await EvaluateAsync(target, ct);
    }

    public async Task<bool> DeleteTargetAsync(Guid companyId, CancellationToken ct = default)
    {
        var target = await _db.CarbonTargets.FirstOrDefaultAsync(t => t.CompanyId == companyId, ct);
        if (target is null)
            return false;

        _db.CarbonTargets.Remove(target);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ------------------------------------------------------------------

    /// <summary>Değerlendirmeleri yanıtları (soru bilgisiyle) ve önerileriyle yükler; tarih sırasıyla döner.</summary>
    private async Task<List<Assessment>> LoadFullAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await _db.Assessments.AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.AssessmentDetails).ThenInclude(d => d.Question)
            .Include(a => a.Recommendations)
            .Where(a => ids.Contains(a.Id))
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

    private async Task<CarbonTrendResponse> BuildCarbonTrendAsync(Guid companyId, CancellationToken ct)
    {
        var series = await _db.CarbonFootprints.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Year)
            .Select(c => new CarbonFootprintSummaryResponse(
                c.Id, c.Year, c.Scope1Tonnes, c.Scope2Tonnes, c.TotalTonnes, c.TonnesPerEmployee))
            .ToListAsync(ct);

        var target = await _db.CarbonTargets.AsNoTracking().FirstOrDefaultAsync(t => t.CompanyId == companyId, ct);
        var targetResponse = target is null
            ? null
            : CarbonTargetEvaluator.Evaluate(target, series.Select(s => new CarbonTargetEvaluator.YearTotal(s.Year, s.TotalTonnes)));

        if (series.Count == 0)
            return new CarbonTrendResponse(series, null, null, null, null, targetResponse);

        var first = series[0];
        var last = series[^1];
        var hasTrend = series.Count >= 2;

        return new CarbonTrendResponse(
            series,
            first.Year,
            last.Year,
            hasTrend ? CarbonCalculator.ChangePercent(first.TotalTonnes, last.TotalTonnes) : null,
            hasTrend ? CarbonCalculator.ChangePercent(first.TonnesPerEmployee, last.TonnesPerEmployee) : null,
            targetResponse);
    }

    private async Task<CarbonTargetResponse> EvaluateAsync(CarbonTarget target, CancellationToken ct)
    {
        var totals = await _db.CarbonFootprints.AsNoTracking()
            .Where(c => c.CompanyId == target.CompanyId)
            .Select(c => new CarbonTargetEvaluator.YearTotal(c.Year, c.TotalTonnes))
            .ToListAsync(ct);
        return CarbonTargetEvaluator.Evaluate(target, totals);
    }

    private async Task EnsureCompanyAsync(Guid companyId, CancellationToken ct)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, ct))
            throw new KeyNotFoundException($"Firma bulunamadı: {companyId}");
    }
}
