namespace LostFoundDocs.Models;

/// <summary>
/// Brouillon de déclaration (formulaire en cours). N'apparaît jamais dans la liste des signalements.
/// <see cref="SubmittedReportId"/> garantit qu'un double appui sur « Déclarer » ne crée pas de doublon.
/// </summary>
public sealed class ReportDraft
{
    public Guid DraftId { get; } = Guid.NewGuid();
    public ReportKind Kind { get; set; }
    public DocumentType? Type { get; set; }
    public string? CustomTitle { get; set; }
    public string HolderName { get; set; } = string.Empty;
    public string? Number { get; set; }
    public DateTime? BirthDate { get; set; }   // facultative
    public string? Issuer { get; set; }
    public DateTime? EventDate { get; set; }
    public string City { get; set; } = "Douala";
    public string Place { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? PhotoPath { get; set; }
    public Guid? SubmittedReportId { get; set; }
}
