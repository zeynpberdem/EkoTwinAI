using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Controllers;

/// <summary>
/// Öneri kütüphanesini (şablonları) listeler. Öneri metinlerinin incelenmesi için;
/// ileride admin panelinde düzenleme uç noktaları eklenebilir.
/// </summary>
[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RecommendationsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Tüm öneri şablonları: önce soru bazlılar (soru sırasıyla), sonra boyut bazlılar.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecommendationTemplateResponse>>> GetAll(CancellationToken ct)
    {
        var templates = await _db.Recommendations
            .AsNoTracking()
            .Include(r => r.Question)
            .ToListAsync(ct);

        var result = templates
            .OrderBy(r => r.TriggerType)
            .ThenBy(r => r.Question?.DisplayOrder ?? 0)
            .ThenBy(r => r.Dimension)
            .Select(r =>
            {
                var dimension = r.Question?.Dimension ?? r.Dimension ?? default;
                return new RecommendationTemplateResponse(
                    r.Id,
                    r.TriggerType,
                    r.Question?.DisplayOrder,
                    dimension,
                    Lookups.DimensionName(dimension),
                    r.Title,
                    r.Rationale,
                    r.CbamNote,
                    r.SimpleTitle,
                    r.SimpleBenefit,
                    r.SimpleCbamNote,
                    Lookups.LevelName(r.Priority),
                    Lookups.LevelName(r.CostLevel),
                    Lookups.LevelName(r.ImpactLevel),
                    Lookups.ImpactTypeName(r.ImpactType),
                    Lookups.DurationText(r.DurationMinMonths, r.DurationMaxMonths),
                    r.Source,
                    r.IsActive);
            })
            .ToList();

        return Ok(result);
    }
}
