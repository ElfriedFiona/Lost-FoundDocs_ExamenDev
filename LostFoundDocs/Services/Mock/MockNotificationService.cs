using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.Services.Mock;

// SIMULATION (frontend uniquement) : notifications locales, aucun SMS ni push.
public sealed class MockNotificationService(IReportService reports) : INotificationService
{
    private readonly List<AppNotification> _items = new();
    private readonly HashSet<Guid> _seeded = new();
    public IReadOnlyList<AppNotification> Notifications
    {
        get
        {
            foreach (var report in reports.Reports.Where(r => r.Status is ReportStatus.MatchFound or ReportStatus.Returned))
                if (_seeded.Add(report.Id) && !_items.Any(n => n.ReportId == report.Id))
                    Add(report, $"{report.StatusLabel} · {report.Title}", report.Status == ReportStatus.Returned ? Routes.Receipt : Routes.Match);
            var ids = reports.Reports.Select(r => r.Id).ToHashSet();
            return _items.Where(n => ids.Contains(n.ReportId)).OrderByDescending(n => n.CreatedAt).ToList();
        }
    }
    public void Add(Report report, string title, string route) => _items.Add(new AppNotification
    { ReportId = report.Id, Title = title, Route = route, CreatedAt = Clock.Now });
    public void MarkAllRead() { foreach (var item in Notifications) item.IsRead = true; }
}
