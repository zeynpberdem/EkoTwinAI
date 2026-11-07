using EkoTwin.Dtos;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;

namespace EkoTwin.Controllers;

/// <summary>
/// Gelişim takibi: "Geçen sefere göre ne değişti? Önerileri yaptık mı? Karbonumuz azalıyor mu?"
/// </summary>
[ApiController]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _service;

    public ProgressController(IProgressService service)
    {
        _service = service;
    }

    /// <summary>Gelişim özeti: değerlendirme geçmişi, son karşılaştırma, öneri takibi, karbon trendi ve hedef.</summary>
    [HttpGet("api/companies/{companyId:guid}/progress")]
    public async Task<ActionResult<ProgressResponse>> GetProgress(Guid companyId, CancellationToken ct)
    {
        var result = await _service.GetProgressAsync(companyId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Aynı firmanın iki değerlendirmesini karşılaştırır (sıra fark etmez; eski olan "önceki" sayılır).</summary>
    [HttpGet("api/assessments/{id:guid}/compare/{otherId:guid}")]
    public async Task<ActionResult<AssessmentComparisonResponse>> Compare(Guid id, Guid otherId, CancellationToken ct)
    {
        try
        {
            var result = await _service.CompareAsync(id, otherId, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ProgressValidationException ex)
        {
            return BadRequest(Invalid("Karşılaştırma yapılamaz", "Assessments", ex));
        }
    }

    /// <summary>Karbon azaltım hedefi ve ilerlemesi.</summary>
    [HttpGet("api/companies/{companyId:guid}/carbon-target")]
    public async Task<ActionResult<CarbonTargetResponse>> GetTarget(Guid companyId, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetTargetAsync(companyId, ct);
            return result is null
                ? NotFound(new ProblemDetails { Title = "Hedef tanımlı değil", Status = 404 })
                : Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Firma bulunamadı", Detail = ex.Message, Status = 404 });
        }
    }

    /// <summary>Karbon hedefini oluşturur veya günceller (firma başına tek hedef).</summary>
    [HttpPut("api/companies/{companyId:guid}/carbon-target")]
    public async Task<ActionResult<CarbonTargetResponse>> SetTarget(Guid companyId, CarbonTargetRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.SetTargetAsync(companyId, request, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Firma bulunamadı", Detail = ex.Message, Status = 404 });
        }
        catch (ProgressValidationException ex)
        {
            return BadRequest(Invalid("Hedef geçersiz", "Target", ex));
        }
    }

    /// <summary>Karbon hedefini siler.</summary>
    [HttpDelete("api/companies/{companyId:guid}/carbon-target")]
    public async Task<IActionResult> DeleteTarget(Guid companyId, CancellationToken ct) =>
        await _service.DeleteTargetAsync(companyId, ct) ? NoContent() : NotFound();

    private static ValidationProblemDetails Invalid(string title, string key, ProgressValidationException ex) =>
        new(new Dictionary<string, string[]> { [key] = ex.Errors.ToArray() }) { Title = title, Status = 400 };
}
