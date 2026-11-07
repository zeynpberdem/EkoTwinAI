using EkoTwin.Dtos;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;

namespace EkoTwin.Controllers;

/// <summary>
/// Karbon hesaplayıcı: "Karbon salınımım ne?" sorusunun cevabı.
/// Firma yıllık fatura değerlerini (elektrik, doğal gaz, yakıtlar) girer; sistem Kapsam 1 + 2 emisyonunu hesaplar.
/// </summary>
[ApiController]
public class CarbonFootprintsController : ControllerBase
{
    private readonly ICarbonFootprintService _service;

    public CarbonFootprintsController(ICarbonFootprintService service)
    {
        _service = service;
    }

    /// <summary>Emisyon faktörleri: kaynak, birim, faktör ve resmi kaynağı (giriş formu bunu kullanır).</summary>
    [HttpGet("api/emission-factors")]
    public async Task<ActionResult<IEnumerable<EmissionFactorResponse>>> GetFactors(CancellationToken ct) =>
        Ok(await _service.GetFactorsAsync(ct));

    /// <summary>Firmanın yıllık karbon kayıtları (yeni yıl önce).</summary>
    [HttpGet("api/companies/{companyId:guid}/carbon-footprints")]
    public async Task<ActionResult<IEnumerable<CarbonFootprintSummaryResponse>>> GetByCompany(Guid companyId, CancellationToken ct)
    {
        var result = await _service.ListByCompanyAsync(companyId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Yeni yıllık hesap. Aynı yıl için kayıt varsa 409 döner.</summary>
    [HttpPost("api/companies/{companyId:guid}/carbon-footprints")]
    public async Task<ActionResult<CarbonFootprintResponse>> Create(Guid companyId, CarbonFootprintRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(companyId, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Firma bulunamadı", Detail = ex.Message, Status = 404 });
        }
        catch (CarbonValidationException ex)
        {
            return BadRequest(Invalid(ex));
        }
        catch (CarbonYearConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = "Bu yıl için kayıt var", Detail = ex.Message, Status = 409 });
        }
    }

    /// <summary>Karbon ayak izi raporu.</summary>
    [HttpGet("api/carbon-footprints/{id:guid}")]
    public async Task<ActionResult<CarbonFootprintResponse>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Hesabı yeni değerlerle yeniden yapar (güncel faktörler ve çalışan sayısıyla).</summary>
    [HttpPut("api/carbon-footprints/{id:guid}")]
    public async Task<ActionResult<CarbonFootprintResponse>> Update(Guid id, CarbonFootprintRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (CarbonValidationException ex)
        {
            return BadRequest(Invalid(ex));
        }
        catch (CarbonYearConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = "Bu yıl için kayıt var", Detail = ex.Message, Status = 409 });
        }
    }

    /// <summary>Hesabı siler.</summary>
    [HttpDelete("api/carbon-footprints/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

    private static ValidationProblemDetails Invalid(CarbonValidationException ex) =>
        new(new Dictionary<string, string[]> { ["Entries"] = ex.Errors.ToArray() })
        {
            Title = "Karbon hesabı girdileri geçersiz",
            Status = 400
        };
}
