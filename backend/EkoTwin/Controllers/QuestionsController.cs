using EkoTwin.Data;
using EkoTwin.Dtos;
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
            .Select(q => new QuestionResponse(q.Id, q.DisplayOrder, q.Dimension, q.Text))
            .ToListAsync(ct);

        return Ok(questions);
    }
}
