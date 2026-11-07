namespace EkoTwin.Services;

/// <summary>Karbon hesabı girdileri iş kurallarına uymadığında (400).</summary>
public class CarbonValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public CarbonValidationException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }
}

/// <summary>Firmanın aynı yıl için zaten bir karbon kaydı varsa (409).</summary>
public class CarbonYearConflictException : Exception
{
    public int Year { get; }

    public CarbonYearConflictException(int year)
        : base($"Bu firmanın {year} yılı için karbon kaydı zaten var. Mevcut kaydı güncelleyin veya silin.")
    {
        Year = year;
    }
}
