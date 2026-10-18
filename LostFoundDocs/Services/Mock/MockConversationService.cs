using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
namespace LostFoundDocs.Services.Mock;

// SIMULATION (frontend uniquement) : messages privés à la session, jamais envoyés.
public sealed class MockConversationService(IReportService reports, IMatchService matches) : IConversationService
{
    private readonly Dictionary<Guid, Conversation> _items = new();
    public IReadOnlyList<Conversation> Conversations
    {
        get
        {
            // Les trois conversations de démonstration sont liées aux mêmes signalements.
            foreach (var report in reports.Reports.Where(r => r.Status is ReportStatus.MatchFound or ReportStatus.Returned).Take(3))
                if (!_items.ContainsKey(report.Id)) Create(report, report.Kind == ReportKind.Lost ? "Brice T." : "Aïssatou N.");
            var ids = reports.Reports.Select(r => r.Id).ToHashSet();
            return _items.Values.Where(c => ids.Contains(c.Report.Id)).OrderByDescending(c => c.Messages.LastOrDefault()?.SentAt).ToList();
        }
    }
    public async Task<Conversation> OpenAsync(Guid reportId)
    {
        var report = await reports.GetAsync(reportId) ?? throw new InvalidOperationException("Signalement introuvable.");
        if (report.Status is not (ReportStatus.MatchFound or ReportStatus.MatchConfirmed or ReportStatus.Returned))
            throw new InvalidOperationException("La conversation nécessite une correspondance active.");
        var match = await matches.GetAsync(reportId) ?? throw new InvalidOperationException("Correspondance introuvable.");
        var conversation = _items.TryGetValue(reportId, out var existing) ? existing : Create(report, match.Contact);
        conversation.Unread = 0;
        return conversation;
    }
    public async Task SendAsync(Guid reportId, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000) throw new ArgumentException("Saisissez un message de 1 à 1 000 caractères.");
        var conversation = await OpenAsync(reportId);
        if (conversation.Report.IsTerminal) throw new InvalidOperationException("Cette conversation est clôturée.");
        conversation.Add(new ChatMessage("Vous", text.Trim(), Clock.Now));
    }
    public async Task AddSystemAsync(Guid reportId, string text)
    {
        var conversation = await OpenAsync(reportId);
        conversation.Add(new ChatMessage("Simulation", text, Clock.Now));
    }
    private Conversation Create(Report report, string contact)
    {
        var conversation = new Conversation { Report = report, Contact = contact, Unread = 1 };
        conversation.Add(new ChatMessage("Simulation", report.Status == ReportStatus.Returned
            ? "Restitution confirmée · " + report.Reference : "Correspondance trouvée. Vous pouvez organiser une restitution.", report.UpdatedAt));
        _items[report.Id] = conversation;
        return conversation;
    }
}
