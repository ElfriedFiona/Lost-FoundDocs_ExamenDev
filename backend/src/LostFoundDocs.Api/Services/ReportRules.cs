using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;

namespace LostFoundDocs.Api.Services;

public static class ReportRules
{
    /// <summary>Change le statut en respectant le cycle de vie ; lève 409 sinon.</summary>
    public static void Transition(Report report, ReportStatus to, DateTime now)
    {
        if (report.Status == to) return;
        if (!ReportStatusRules.CanTransition(report.Status, to))
            throw ApiException.Conflict("invalid_transition", $"Ce signalement ne peut pas passer de « {report.Status} » à « {to} ».");
        report.Status = to;
        report.UpdatedAt = now;
    }

    public static ReportFacts Facts(Report r) => new(
        r.Type, r.Type == DocumentType.Other ? TextNormalizer.Normalize(r.CustomTitle) : "", r.HolderNameNormalized, r.NumberHash,
        r.BirthDateHash, r.IssuerNormalized, TextNormalizer.Normalize(r.City), r.EventDate);

    public static ReportDto ToDto(Report r, Guid? matchId) => new(
        r.Id, r.Reference, r.Kind, r.Type, DocumentTitles.Of(r), r.CustomTitle, r.HolderName,
        r.NumberLast4 is null ? null : "•••• " + r.NumberLast4, r.BirthYear, r.Issuer, DateOnly.FromDateTime(r.EventDate), r.City, r.Place,
        r.Notes, r.PhotoFile is not null, r.Status, r.CreatedAt, r.UpdatedAt, matchId);
}
