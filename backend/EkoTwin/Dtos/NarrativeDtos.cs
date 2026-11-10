using EkoTwin.Models;

namespace EkoTwin.Dtos;

/// <summary>Rapor mektubu. Arayüz Text'i gösterir; SourceName kullanıcıya şeffaflık için küçük bir notla yazılabilir.</summary>
public record NarrativeResponse(
    Guid Id,
    Guid AssessmentId,
    string Text,
    NarrativeSource Source,
    string SourceName,
    string Provider,
    string? Model,
    string PromptVersion,
    string? FallbackReason,
    DateTime CreatedAt)
{
    public static NarrativeResponse From(AssessmentNarrative n) => new(
        n.Id, n.AssessmentId, n.Text, n.Source,
        n.Source == NarrativeSource.Ai ? "Yapay zekâ ile yazıldı, otomatik kontrolden geçti" : "Kural tabanlı metin",
        n.Provider, n.Model, n.PromptVersion, n.FallbackReason, n.CreatedAt);
}
