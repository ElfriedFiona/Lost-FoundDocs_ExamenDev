namespace LostFoundDocs.Api.Domain;

/// <summary>Cycle de vie d'un signalement (identique à celui du frontend, avec retour à « Recherche » après contestation).</summary>
public static class ReportStatusRules
{
    public const int ExpiryDays = 90;

    private static readonly Dictionary<ReportStatus, ReportStatus[]> Allowed = new()
    {
        [ReportStatus.Searching] = new[] { ReportStatus.MatchFound, ReportStatus.Cancelled, ReportStatus.Expired },
        [ReportStatus.MatchFound] = new[] { ReportStatus.MatchConfirmed, ReportStatus.MatchDisputed, ReportStatus.Searching, ReportStatus.Cancelled, ReportStatus.Expired },
        [ReportStatus.MatchDisputed] = new[] { ReportStatus.Searching, ReportStatus.Returned, ReportStatus.Cancelled },
        [ReportStatus.MatchConfirmed] = new[] { ReportStatus.Returned, ReportStatus.MatchFound, ReportStatus.Searching, ReportStatus.Cancelled },
        [ReportStatus.Returned] = Array.Empty<ReportStatus>(),
        [ReportStatus.Expired] = Array.Empty<ReportStatus>(),
        [ReportStatus.Cancelled] = Array.Empty<ReportStatus>(),
    };

    public static bool CanTransition(ReportStatus from, ReportStatus to) => Allowed[from].Contains(to);
    public static bool IsTerminal(ReportStatus status) => Allowed[status].Length == 0;
}
