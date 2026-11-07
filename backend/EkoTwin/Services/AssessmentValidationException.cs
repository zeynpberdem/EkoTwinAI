namespace EkoTwin.Services;

/// <summary>
/// Gönderilen yanıtlar iş kurallarına uymadığında fırlatılır (eksik/fazla/tekrarlı soru vb.).
/// Controller bunu 400 Bad Request olarak döndürür.
/// </summary>
public class AssessmentValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public AssessmentValidationException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }
}
