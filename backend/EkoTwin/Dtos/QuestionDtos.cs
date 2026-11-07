using EkoTwin.Models;

namespace EkoTwin.Dtos;

/// <summary>
/// Ankette gösterilecek soru (arayüzde bir kart).
/// SimpleText kartta gösterilir, HelpText ⓘ simgesiyle açılır, Text resmi metindir.
/// </summary>
public record QuestionResponse(
    Guid Id,
    int DisplayOrder,
    Dimension Dimension,
    string DimensionName,
    string DimensionSimpleName,
    string Text,
    string SimpleText,
    string? HelpText,
    IReadOnlyList<AnswerOptionResponse> Options);

/// <summary>Cevap seçeneği: kullanıcı Label'ı görür, istekte Score (1-5) gönderilir.</summary>
public record AnswerOptionResponse(int Score, string Label);
