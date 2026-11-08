using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Services;

public interface IBenchmarkService
{
    /// <summary>Firmanın sektör karşılaştırması; firma yoksa null.</summary>
    Task<BenchmarkResponse?> GetAsync(Guid companyId, CancellationToken ct = default);
}

public class BenchmarkService : IBenchmarkService
{
    private readonly AppDbContext _db;

    public BenchmarkService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BenchmarkResponse?> GetAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null)
            return null;

        var size = BenchmarkCalculator.SizeOf(company.EmployeeCount);
        var highlights = new List<string>();

        // ---- Puan karşılaştırması: her firmanın en son değerlendirmesi ----
        var latestScores = await LatestScoresAsync(ct);
        var you = latestScores.FirstOrDefault(r => r.CompanyId == companyId);

        BenchmarkGroupResponse? group = null;
        var metrics = new List<BenchmarkMetricResponse>();

        if (you is null)
        {
            highlights.Add("Karşılaştırma için önce testi doldurun.");
        }
        else
        {
            // Firmanın güncel sektör ve büyüklüğü kullanılır (kayıt güncellenmiş olabilir)
            you = you with { Sector = company.Sector, Size = size };
            var selected = BenchmarkCalculator.SelectGroup(companyId, company.Sector, size, latestScores,
                r => r.CompanyId, r => r.Sector, r => r.Size);

            if (selected is { } s)
            {
                group = new BenchmarkGroupResponse(s.Level, s.Peers.Count,
                    BenchmarkCalculator.GroupDescription(s.Level, company.Sector, size, s.Peers.Count));
                metrics = BenchmarkCalculator.CompareScores(you, s.Peers);

                highlights.Add(group.SimpleDescription);
                highlights.Add(metrics[0].SimpleText); // genel puan
                var best = metrics.Skip(1).OrderByDescending(m => m.BetterThanPercent).First();
                var worst = metrics.Skip(1).OrderBy(m => m.BetterThanPercent).First();
                if (best.Position == BenchmarkPosition.Above)
                    highlights.Add($"En güçlü olduğunuz konu: {best.SimpleName}.");
                if (worst.Position == BenchmarkPosition.Below)
                    highlights.Add($"Benzer firmaların gerisinde kaldığınız konu: {worst.SimpleName}.");
            }
            else
            {
                highlights.Add(BenchmarkCalculator.NotEnoughPeers);
            }
        }

        // ---- Karbon karşılaştırması: her firmanın en son karbon kaydı ----
        var carbon = await CompareCarbonAsync(company, size, ct);
        if (carbon is not null)
            highlights.Add(carbon.SimpleText);

        // ---- Türkiye geneli resmi referanslar (yalnızca doğrulanmış) ----
        var answers = await LatestAnswersAsync(companyId, ct);
        var references = await _db.BenchmarkReferences.AsNoTracking().ToListAsync(ct);
        var national = BenchmarkCalculator.NationalReferences(size, references, answers);

        return new BenchmarkResponse(
            companyId, size, BenchmarkCalculator.SizeClassName(size), you is not null,
            group, metrics, carbon, national, highlights);
    }

    // ------------------------------------------------------------------

    /// <summary>Her firmanın en son değerlendirmesinin skorları (sektör ve büyüklükle birlikte).</summary>
    private async Task<List<BenchmarkCalculator.ScoreRow>> LatestScoresAsync(CancellationToken ct)
    {
        var rows = await _db.Assessments.AsNoTracking()
            .Select(a => new
            {
                a.CompanyId, a.CreatedAt, a.Company.Sector, a.Company.EmployeeCount,
                a.TotalScore, a.DigitalInfrastructureScore, a.EnergyEfficiencyScore,
                a.CarbonManagementScore, a.SupplyChainScore
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.CompanyId)
            .Select(g => g.OrderByDescending(r => r.CreatedAt).First())
            .Select(r => new BenchmarkCalculator.ScoreRow(
                r.CompanyId, r.Sector, BenchmarkCalculator.SizeOf(r.EmployeeCount),
                r.TotalScore, r.DigitalInfrastructureScore, r.EnergyEfficiencyScore,
                r.CarbonManagementScore, r.SupplyChainScore))
            .ToList();
    }

    private async Task<CarbonBenchmarkResponse?> CompareCarbonAsync(Company company, SizeClass size, CancellationToken ct)
    {
        var rows = await _db.CarbonFootprints.AsNoTracking()
            .Where(c => c.TonnesPerEmployee > 0)
            .Select(c => new { c.CompanyId, c.Year, c.TonnesPerEmployee, c.Company.Sector, c.Company.EmployeeCount })
            .ToListAsync(ct);

        var latest = rows
            .GroupBy(r => r.CompanyId)
            .Select(g => g.OrderByDescending(r => r.Year).First())
            .Select(r => new BenchmarkCalculator.CarbonRow(
                r.CompanyId, r.Sector, BenchmarkCalculator.SizeOf(r.EmployeeCount), r.Year, r.TonnesPerEmployee))
            .ToList();

        var you = latest.FirstOrDefault(r => r.CompanyId == company.Id);
        if (you is null)
            return null;

        var selected = BenchmarkCalculator.SelectGroup(company.Id, company.Sector, size, latest,
            r => r.CompanyId, r => r.Sector, r => r.Size);
        if (selected is not { } s)
            return null;

        var group = new BenchmarkGroupResponse(s.Level, s.Peers.Count,
            BenchmarkCalculator.GroupDescription(s.Level, company.Sector, size, s.Peers.Count));
        return BenchmarkCalculator.CompareCarbon(you with { Sector = company.Sector, Size = size }, group, s.Peers);
    }

    /// <summary>Firmanın en son değerlendirmesindeki cevaplar: soru numarası → puan.</summary>
    private async Task<Dictionary<int, int>> LatestAnswersAsync(Guid companyId, CancellationToken ct)
    {
        var latestId = await _db.Assessments.AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);

        if (latestId is null)
            return new Dictionary<int, int>();

        return await _db.AssessmentDetails.AsNoTracking()
            .Where(d => d.AssessmentId == latestId)
            .ToDictionaryAsync(d => d.Question.DisplayOrder, d => d.Score, ct);
    }
}
