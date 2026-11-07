using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Controllers;

[ApiController]
[Route("api/questions")]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuestionsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Ankette gösterilecek aktif soruları sırasıyla getirir.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestionResponse>>> GetAll(CancellationToken ct)
    {
        var questions = await _db.Questions
            .AsNoTracking()
            .Where(q => q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .Include(q => q.Options)
            .ToListAsync(ct);

        return Ok(questions.Select(q => new QuestionResponse(
            q.Id,
            q.DisplayOrder,
            q.Dimension,
            ScoreCalculator.DisplayName(q.Dimension),
            ScoreCalculator.SimpleName(q.Dimension),
            q.Text,
            q.SimpleText,
            q.HelpText,
            q.Options.OrderBy(o => o.Score).Select(o => new AnswerOptionResponse(o.Score, o.Label)).ToList())));
    }
}
