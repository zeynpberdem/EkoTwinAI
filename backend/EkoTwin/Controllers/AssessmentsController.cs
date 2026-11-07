using EkoTwin.Dtos;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;

namespace EkoTwin.Controllers;

[ApiController]
[Route("api/assessments")]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _service;

    public AssessmentsController(IAssessmentService service)
    {
        _service = service;
    }

    /// <summary>Yanıtları alır, skorları hesaplar ve değerlendirmeyi kaydeder.</summary>
    [HttpPost]
    public async Task<ActionResult<AssessmentResponse>> Create(CreateAssessmentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Firma bulunamadı", Detail = ex.Message, Status = 404 });
        }
        catch (AssessmentValidationException ex)
        {
            var problem = new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["Answers"] = ex.Errors.ToArray() })
            {
                Title = "Yanıtlar geçersiz",
                Status = 400
            };
            return BadRequest(problem);
        }
    }

    /// <summary>Değerlendirme raporunu getirir: genel skor, 4 boyut skoru ve tüm yanıtlar.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssessmentResponse>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
