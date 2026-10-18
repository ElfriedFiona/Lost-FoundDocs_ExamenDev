using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Abstractions;

public interface IReportService
{
    /// <summary>Signalements de l'utilisateur connecté, du plus récent au plus ancien.</summary>
    IReadOnlyList<Report> Reports { get; }
    event EventHandler? ReportsChanged;

    Task<Report?> GetAsync(Guid id);
    Task ResetAsync(bool populated);

    /// <summary>
    /// Enregistre le brouillon UNE SEULE FOIS (idempotent : un second appel avec le même brouillon
    /// renvoie le signalement déjà créé, sans doublon).
    /// </summary>
    Task<Report> SubmitAsync(ReportDraft draft);

    /// <summary>Fait évoluer le statut du même signalement (transitions contrôlées, aucun doublon créé).</summary>
    Task<Report> UpdateStatusAsync(Guid id, ReportStatus newStatus);

    /// <summary>Annule un signalement si le cycle de vie l'autorise.</summary>
    Task<bool> CancelAsync(Guid id);
}
