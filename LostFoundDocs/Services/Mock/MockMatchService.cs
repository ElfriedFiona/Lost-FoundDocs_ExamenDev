using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.Services.Mock;

// SIMULATION (frontend uniquement) : correspondances déterministes, sans recherche réseau.
public sealed class MockMatchService(IReportService reports, INotificationService notifications) : IMatchService
{
    private readonly Dictionary<Guid, MatchResult> _matches = new();
    public async Task<MatchResult?> SearchAsync(Guid reportId)
    {
        var report = await RequireReport(reportId);
        if (report.Status != ReportStatus.Searching) return await GetAsync(reportId);
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        var compatible = report.City == "Douala" && (report.Kind == ReportKind.Lost
            ? report.Type is DocumentType.Diploma or DocumentType.NationalId
            : report.Type is DocumentType.Passport or DocumentType.Diploma);
        var found = DemoSettings.MatchMode == MatchMode.AlwaysMatch ||
            (DemoSettings.MatchMode == MatchMode.Auto && compatible);
        if (!found) return null;
        var match = CreateMatch(report);
        _matches[report.Id] = match;
        await reports.UpdateStatusAsync(report.Id, ReportStatus.MatchFound);
        notifications.Add(report, $"Correspondance trouvée · {report.Title}", Routes.Match);
        return match;
    }
    public async Task<MatchResult?> GetAsync(Guid reportId)
    {
        var report = await RequireReport(reportId);
        if (report.Status is ReportStatus.Searching or ReportStatus.Cancelled or ReportStatus.Expired) return null;
        if (!_matches.TryGetValue(reportId, out var match)) _matches[reportId] = match = CreateMatch(report);
        return match;
    }
    public async Task DisputeAsync(Guid reportId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Choisissez un motif.");
        var report = await reports.UpdateStatusAsync(reportId, ReportStatus.MatchDisputed);
        notifications.Add(report, "Correspondance contestée : " + reason, Routes.ReportDetails);
    }
    private async Task<Report> RequireReport(Guid id) => await reports.GetAsync(id)
        ?? throw new InvalidOperationException("Signalement introuvable pour cette session.");
    private static MatchResult CreateMatch(Report report)
    {
        var eventDate = report.Kind == ReportKind.Lost
            ? report.EventDate.AddDays(1) : report.EventDate.AddDays(-1);
        if (eventDate > Clock.Today) eventDate = Clock.Today;
        return new MatchResult(report.Id, 89 + (int)report.Type % 6,
            report.Kind == ReportKind.Lost ? "Brice T." : "Aïssatou N.", eventDate);
    }
}
