using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

/// <summary>
/// Recherche continue : à chaque nouveau signalement (ou retour en recherche), il est comparé aux signalements actifs du camp opposé.
/// La meilleure correspondance au-dessus du seuil est enregistrée, les deux parties sont notifiées et une conversation est ouverte.
/// Les opérations sont sérialisées (un seul serveur) et protégées en base par une contrainte d'unicité de la paire.
/// </summary>
public sealed class MatchingService(AppDbContext db, TimeProvider time, IOptions<MatchingOptions> options, NotificationService notifications,
                                    ConversationService conversations, MatchQueries queries)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task<Match?> TryMatchAsync(Guid reportId)
    {
        await Gate.WaitAsync();
        try { return await TryMatchCoreAsync(reportId); }
        finally { Gate.Release(); }
    }

    private async Task<Match?> TryMatchCoreAsync(Guid reportId)
    {
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report is null || report.Status != ReportStatus.Searching) return null;

        var opposite = report.Kind == ReportKind.Lost ? ReportKind.Found : ReportKind.Lost;
        var type = report.Type;
        var userId = report.UserId;
        var candidates = await db.Reports
            .Where(r => r.Kind == opposite && r.Status == ReportStatus.Searching && r.UserId != userId && r.Type == type)
            .ToListAsync();

        var previous = await db.Matches.Where(m => m.LostReportId == report.Id || m.FoundReportId == report.Id).ToListAsync();
        var excluded = previous.Select(m => m.LostReportId == report.Id ? m.FoundReportId : m.LostReportId).ToHashSet();

        Report? bestReport = null;
        MatchEvaluation? best = null;
        foreach (var candidate in candidates.Where(c => !excluded.Contains(c.Id)))
        {
            var (lost, found) = report.Kind == ReportKind.Lost ? (report, candidate) : (candidate, report);
            var evaluation = MatchScorer.Evaluate(ReportRules.Facts(lost), ReportRules.Facts(found), options.Value);
            if (evaluation is null) continue;
            if (best is null || evaluation.Score > best.Score || (evaluation.Score == best.Score && candidate.CreatedAt < bestReport!.CreatedAt))
            {
                best = evaluation;
                bestReport = candidate;
            }
        }
        if (best is null || bestReport is null) return null;

        var lostReport = report.Kind == ReportKind.Lost ? report : bestReport;
        var foundReport = report.Kind == ReportKind.Found ? report : bestReport;
        var now = Now;

        var match = new Match
        {
            Id = Guid.NewGuid(), LostReportId = lostReport.Id, FoundReportId = foundReport.Id, Score = best.Score,
            CriteriaJson = best.CriteriaJson, Status = MatchStatus.Active, CreatedAt = now,
        };
        db.Matches.Add(match);
        ReportRules.Transition(lostReport, ReportStatus.MatchFound, now);
        ReportRules.Transition(foundReport, ReportStatus.MatchFound, now);

        var conversation = conversations.CreateForMatch(match, lostReport, foundReport);
        notifications.Add(lostReport.UserId, NotificationKind.Match,
            $"Une correspondance a été trouvée pour votre {DocumentTitles.Of(lostReport).ToLowerInvariant()}.",
            "Ouvrez la correspondance pour contacter la personne qui l’a trouvée.", lostReport.Id, match.Id, conversation.Id);
        notifications.Add(foundReport.UserId, NotificationKind.Match,
            $"Le propriétaire présumé du document « {DocumentTitles.Of(foundReport)} » a été notifié.",
            "Il peut maintenant vous écrire pour organiser la restitution.", foundReport.Id, match.Id, conversation.Id);

        await db.SaveChangesAsync();
        return match;
    }

    /// <summary>Résultat de recherche pour un signalement de l'utilisateur : correspondance existante, ou nouvelle tentative.</summary>
    public async Task<SearchResultDto> SearchAsync(Guid userId, Guid reportId)
    {
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId)
                     ?? throw ApiException.NotFound("Signalement introuvable.");

        if (report.Status == ReportStatus.MatchDisputed)
        {
            ReportRules.Transition(report, ReportStatus.Searching, Now);
            await db.SaveChangesAsync();
        }
        if (report.Status == ReportStatus.Searching) await TryMatchAsync(reportId);

        report = await db.Reports.FirstAsync(r => r.Id == reportId);
        var match = await queries.CurrentMatchAsync(report);
        return match is null ? new SearchResultDto(false, null) : new SearchResultDto(true, await queries.BuildDtoAsync(match, userId));
    }

    /// <summary>
    /// Contestation par un participant : sa propre fiche passe en « contesté », l'autre fiche repasse en recherche (le document
    /// reste à rendre) et la conversation est close. La paire ne pourra plus être reproposée.
    /// </summary>
    public async Task<MatchDto> DisputeAsync(Guid userId, Guid matchId, DisputeRequest req)
    {
        var reason = (req.Reason ?? "").Trim();
        var v = new Validator();
        if (reason.Length == 0) v.Add("reason", "Choisissez un motif pour continuer.");
        else if (reason.Length > 200) v.Add("reason", "200 caractères maximum.");
        v.ThrowIfInvalid();

        var (match, mine, other) = await queries.LoadForParticipantAsync(userId, matchId);
        if (match.Status != MatchStatus.Active)
            throw ApiException.Conflict("match_not_disputable", "Cette correspondance ne peut plus être contestée.");

        var now = Now;
        match.Status = MatchStatus.Disputed;
        match.DisputedByUserId = userId;
        match.DisputeReason = reason;
        match.DisputedAt = now;
        ReportRules.Transition(mine, ReportStatus.MatchDisputed, now);
        ReportRules.Transition(other, ReportStatus.Searching, now);

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id);
        if (conversation is not null)
        {
            conversation.ClosedAt = now;
            conversations.AddSystemMessage(conversation.Id, "La correspondance a été contestée : la conversation est clôturée.");
        }

        notifications.Add(other.UserId, NotificationKind.Match,
            $"La correspondance pour « {DocumentTitles.Of(other)} » a été contestée.",
            "La recherche reprend automatiquement.", other.Id, match.Id, conversation?.Id);
        await db.SaveChangesAsync();

        await TryMatchAsync(other.Id);
        return await queries.BuildDtoAsync(match, userId);
    }
}
