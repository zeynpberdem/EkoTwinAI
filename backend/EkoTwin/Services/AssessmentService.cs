using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Services;

public class AssessmentService : IAssessmentService
{
    private readonly AppDbContext _db;

    public AssessmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AssessmentResponse> CreateAsync(CreateAssessmentRequest request, CancellationToken ct = default)
    {
        // 1) Firma var mı?
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new KeyNotFoundException($"Firma bulunamadı: {request.CompanyId}");

        // 2) Yanıtlar, aktif soruların tamamını tam olarak bir kez kapsıyor mu?
        var activeQuestions = await _db.Questions
            .Where(q => q.IsActive)
            .ToDictionaryAsync(q => q.Id, ct);

        ValidateAnswers(request.Answers, activeQuestions);

        // 3) Boyut skorlarını hesapla
        var scoresByDimension = request.Answers
            .GroupBy(a => activeQuestions[a.QuestionId].Dimension)
            .ToDictionary(g => g.Key, g => ScoreCalculator.DimensionScore(g.Select(a => a.Score)));

        decimal ScoreOf(Dimension d) => scoresByDimension.GetValueOrDefault(d);

        var assessment = new Assessment
        {
            CompanyId = request.CompanyId,
            CreatedAt = DateTime.UtcNow,
            DigitalInfrastructureScore = ScoreOf(Dimension.DigitalInfrastructure),
            EnergyEfficiencyScore = ScoreOf(Dimension.EnergyEfficiency),
            CarbonManagementScore = ScoreOf(Dimension.CarbonManagement),
            SupplyChainScore = ScoreOf(Dimension.SupplyChain),
            TotalScore = ScoreCalculator.TotalScore(scoresByDimension.Values),
            AssessmentDetails = request.Answers
                .Select(a => new AssessmentDetail { QuestionId = a.QuestionId, Score = a.Score })
                .ToList()
        };

        // 4) Değerlendirme + 16 yanıt tek bir işlemde (transaction) kaydedilir.
        _db.Assessments.Add(assessment);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(assessment.Id, ct))!;
    }

    public async Task<AssessmentResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(a => a.Company)
            .Include(a => a.AssessmentDetails)
                .ThenInclude(d => d.Question)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return assessment is null ? null : ToResponse(assessment);
    }

    public async Task<IReadOnlyList<AssessmentSummaryResponse>?> GetByCompanyAsync(Guid companyId, CancellationToken ct = default)
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId, ct);
        if (!companyExists)
            return null;

        var assessments = await _db.Assessments
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return assessments
            .Select(a => new AssessmentSummaryResponse(
                a.Id,
                a.CreatedAt,
                a.TotalScore,
                ScoreCalculator.MaturityLevel(a.TotalScore),
                a.DigitalInfrastructureScore,
                a.EnergyEfficiencyScore,
                a.CarbonManagementScore,
                a.SupplyChainScore))
            .ToList();
    }

    // ------------------------------------------------------------------

    private static void ValidateAnswers(
        IReadOnlyCollection<AnswerRequest> answers,
        IReadOnlyDictionary<Guid, Question> activeQuestions)
    {
        var errors = new List<string>();

        var duplicates = answers
            .GroupBy(a => a.QuestionId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
            errors.Add($"Aynı soru birden fazla kez yanıtlanmış: {string.Join(", ", duplicates)}");

        var unknown = answers
            .Select(a => a.QuestionId)
            .Where(id => !activeQuestions.ContainsKey(id))
            .Distinct()
            .ToList();
        if (unknown.Count > 0)
            errors.Add($"Geçersiz veya pasif soru: {string.Join(", ", unknown)}");

        var answeredIds = answers.Select(a => a.QuestionId).ToHashSet();
        var missing = activeQuestions.Values
            .Where(q => !answeredIds.Contains(q.Id))
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.DisplayOrder)
            .ToList();
        if (missing.Count > 0)
            errors.Add($"Yanıtlanmamış soru numaraları: {string.Join(", ", missing)}");

        var outOfRange = answers.Where(a => a.Score is < 1 or > 5).ToList();
        if (outOfRange.Count > 0)
            errors.Add("Puanlar 1 ile 5 arasında olmalıdır.");

        if (errors.Count > 0)
            throw new AssessmentValidationException(errors);
    }

    private static AssessmentResponse ToResponse(Assessment a)
    {
        var dimensionScores = new[]
        {
            (Dimension.DigitalInfrastructure, a.DigitalInfrastructureScore),
            (Dimension.EnergyEfficiency, a.EnergyEfficiencyScore),
            (Dimension.CarbonManagement, a.CarbonManagementScore),
            (Dimension.SupplyChain, a.SupplyChainScore)
        }
        .Select(x => new DimensionScoreResponse(
            x.Item1,
            ScoreCalculator.DisplayName(x.Item1),
            x.Item2,
            ScoreCalculator.MaturityLevel(x.Item2)))
        .ToList();

        var answers = a.AssessmentDetails
            .OrderBy(d => d.Question.DisplayOrder)
            .Select(d => new AnswerResponse(
                d.QuestionId,
                d.Question.DisplayOrder,
                d.Question.Dimension,
                d.Question.Text,
                d.Score))
            .ToList();

        return new AssessmentResponse(
            a.Id,
            a.CompanyId,
            a.Company.CompanyName,
            a.CreatedAt,
            a.TotalScore,
            ScoreCalculator.MaturityLevel(a.TotalScore),
            dimensionScores,
            answers);
    }
}
