using System.Text.Json;
using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

/// <summary>Lectures liées aux correspondances : correspondance « courante » d'un signalement et vue de l'utilisateur.</summary>
public sealed class MatchQueries(AppDbContext db, IOptions<MatchingOptions> options)
{
    /// <summary>
    /// Une correspondance contestée n'est « courante » que pour celui qui l'a contestée tant que son signalement est en statut « contesté » ;
    /// ensuite (retour en recherche) elle n'est plus affichée.
    /// </summary>
    public static bool IsCurrent(Match m, Report r) =>
        m.Status is MatchStatus.Active or MatchStatus.Confirmed or MatchStatus.Returned
        || (m.Status == MatchStatus.Disputed && r.Status == ReportStatus.MatchDisputed && m.DisputedByUserId == r.UserId);

    public async Task<Match?> CurrentMatchAsync(Report report)
    {
        var matches = await db.Matches.Where(m => m.LostReportId == report.Id || m.FoundReportId == report.Id)
            .OrderByDescending(m => m.CreatedAt).ToListAsync();
        return matches.FirstOrDefault(m => IsCurrent(m, report));
    }

    public async Task<Dictionary<Guid, Match>> CurrentMatchesAsync(IReadOnlyCollection<Report> reports)
    {
        var ids = reports.Select(r => r.Id).ToList();
        var matches = await db.Matches.Where(m => ids.Contains(m.LostReportId) || ids.Contains(m.FoundReportId)).ToListAsync();
        var result = new Dictionary<Guid, Match>();
        foreach (var r in reports)
        {
            var current = matches.Where(m => (m.LostReportId == r.Id || m.FoundReportId == r.Id) && IsCurrent(m, r))
                .OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            if (current is not null) result[r.Id] = current;
        }
        return result;
    }

    /// <summary>Charge une correspondance dont l'utilisateur est l'un des deux participants (sinon 404).</summary>
    public async Task<(Match Match, Report Mine, Report Other)> LoadForParticipantAsync(Guid userId, Guid matchId)
    {
        var match = await db.Matches.FirstOrDefaultAsync(m => m.Id == matchId) ?? throw ApiException.NotFound("Correspondance introuvable.");
        var lost = await db.Reports.FirstAsync(r => r.Id == match.LostReportId);
        var found = await db.Reports.FirstAsync(r => r.Id == match.FoundReportId);
        if (lost.UserId == userId) return (match, lost, found);
        if (found.UserId == userId) return (match, found, lost);
        throw ApiException.NotFound("Correspondance introuvable.");
    }

    public async Task<MatchDto> BuildDtoAsync(Match match, Guid viewerUserId)
    {
        var (_, mine, other) = await LoadForParticipantAsync(viewerUserId, match.Id);
        var otherUser = await db.Users.FirstOrDefaultAsync(u => u.Id == other.UserId);
        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id);
        var criteria = JsonSerializer.Deserialize<List<CriterionJson>>(match.CriteriaJson) ?? new();

        return new MatchDto(
            match.Id, mine.Id, mine.Kind, other.Reference, otherUser is null ? "Utilisateur" : Names.Short(otherUser.FullName), mine.Type,
            mine.City, DateOnly.FromDateTime(other.EventDate), match.Score,
            match.Score >= options.Value.StrongScore ? "Correspondance forte" : "Correspondance probable",
            criteria.Select(c => new CriterionDto(c.name, c.state)).ToList(), match.Status,
            match.Status == MatchStatus.Disputed, match.DisputeReason, match.CreatedAt, conversation?.Id);
    }

    private sealed record CriterionJson(string name, string state);
}
