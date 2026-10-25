using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Abstractions;

public interface IMatchService
{
    MatchOutcome? GetOutcome(Guid reportId);
    MatchResult? GetMatch(Guid reportId);

    /// <summary>Lance la recherche. Idempotent : un signalement déjà recherché renvoie le même résultat (sauf force).</summary>
    Task<MatchOutcome> SearchAsync(Guid reportId, bool force = false);

    Task ReportErrorAsync(Guid reportId, string reason);
}
