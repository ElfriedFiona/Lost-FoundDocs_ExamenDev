namespace LostFoundDocs.Models;

/// <summary>Cycle de vie : un signalement évolue en changeant de statut, jamais en se dupliquant.</summary>
public static class ReportStatusRules
{
    public const int ExpiryDays = 90;

    private static readonly Dictionary<ReportStatus, ReportStatus[]> Allowed = new()
    {
        [ReportStatus.Searching] = new[] { ReportStatus.MatchFound, ReportStatus.Cancelled, ReportStatus.Expired },
        [ReportStatus.MatchFound] = new[] { ReportStatus.MatchConfirmed, ReportStatus.MatchDisputed, ReportStatus.Cancelled, ReportStatus.Expired },
        [ReportStatus.MatchDisputed] = new[] { ReportStatus.Searching, ReportStatus.Returned, ReportStatus.Cancelled },
        [ReportStatus.MatchConfirmed] = new[] { ReportStatus.Returned, ReportStatus.Cancelled },
        [ReportStatus.Returned] = Array.Empty<ReportStatus>(),
        [ReportStatus.Expired] = Array.Empty<ReportStatus>(),
        [ReportStatus.Cancelled] = Array.Empty<ReportStatus>(),
    };

    public static bool CanTransition(ReportStatus from, ReportStatus to) => Allowed[from].Contains(to);

    public static bool IsTerminal(ReportStatus status) => Allowed[status].Length == 0;

    public static string Label(ReportStatus status) => status switch
    {
        ReportStatus.Searching => "Recherche en cours",
        ReportStatus.MatchFound => "Correspondance trouvée",
        ReportStatus.MatchConfirmed => "Correspondance confirmée",
        ReportStatus.MatchDisputed => "Correspondance contestée",
        ReportStatus.Returned => "Restitué",
        ReportStatus.Expired => "Expiré / Archivé",
        ReportStatus.Cancelled => "Annulé",
        _ => status.ToString(),
    };
}
