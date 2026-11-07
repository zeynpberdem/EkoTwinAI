using EkoTwin.Models;

namespace EkoTwin.Dtos;

/// <summary>Ankette gösterilecek soru.</summary>
public record QuestionResponse(
    Guid Id,
    int DisplayOrder,
    Dimension Dimension,
    string Text);
