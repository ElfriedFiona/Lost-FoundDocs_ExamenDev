using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// SIMULATION (frontend uniquement) : recherche de correspondance locale (voir <see cref="MatchSimulator"/>).
/// Idempotent : chercher deux fois le même signalement ne crée ni deuxième conversation ni deuxième notification.
/// </summary>
public sealed class MockMatchService : IMatchService
{
    private readonly IReportService _reports;
    private readonly DemoWorld _world;
    private readonly IConversationService _conversations;
    private readonly INotificationService _notifications;

    public MockMatchService(IReportService reports, DemoWorld world, IConversationService conversations, INotificationService notifications)
    {
        _reports = reports;
        _world = world;
        _conversations = conversations;
        _notifications = notifications;
    }

    public MatchOutcome? GetOutcome(Guid reportId)
    {
        lock (_world.Gate) { return _world.Current.Outcomes.GetValueOrDefault(reportId); }
    }

    public MatchResult? GetMatch(Guid reportId)
    {
        lock (_world.Gate) { return _world.Current.Matches.GetValueOrDefault(reportId); }
    }

    public async Task<MatchOutcome> SearchAsync(Guid reportId, bool force = false)
    {
        await Task.Delay(DemoSettings.SimulatedLatencyMs);
        var report = await _reports.GetAsync(reportId) ?? throw new KeyNotFoundException("Signalement introuvable.");

        lock (_world.Gate)
        {
            var world = _world.Current;
            if (world.Outcomes.TryGetValue(reportId, out var existing) && (!force || existing.Found)) return existing;
        }

        var (found, counterpart) = MatchSimulator.Evaluate(report, DemoSettings.MatchMode);
        var searchable = report.Status == ReportStatus.Searching;
        if (!found || !searchable)
        {
            var none = new MatchOutcome(false, null);
            lock (_world.Gate) { _world.Current.Outcomes[reportId] = none; }
            return none;
        }

        await _reports.UpdateStatusAsync(reportId, ReportStatus.MatchFound);
        var now = Clock.Now;
        var match = new MatchResult
        {
            ReportId = reportId,
            UserSide = report.Kind,
            CounterpartReference = MatchSimulator.CounterpartReference(report.Reference, now.Year),
            CounterpartName = counterpart,
            Type = report.Type,
            City = report.City,
            CounterpartDate = MatchSimulator.CounterpartDate(report, now),
            Score = MatchSimulator.ScoreFor(report.Reference),
            Criteria = MatchSimulator.BuildCriteria(report),
            FoundAt = now,
        };
        var outcome = new MatchOutcome(true, match);
        lock (_world.Gate)
        {
            var world = _world.Current;
            world.Matches[reportId] = match;
            world.Outcomes[reportId] = outcome;
        }

        var conversation = await _conversations.EnsureForMatchAsync(report, match);
        var title = report.Kind == ReportKind.Lost
            ? $"Une correspondance a été trouvée pour votre {report.Title.ToLowerInvariant()}."
            : $"Le propriétaire du document « {report.Title} » a été notifié.";
        await _notifications.AddAsync(NotificationKind.Match, title, "Ouvrez la correspondance pour organiser la restitution.", reportId, conversation.Id);
        return outcome;
    }

    public async Task ReportErrorAsync(Guid reportId, string reason)
    {
        var match = GetMatch(reportId) ?? throw new InvalidOperationException("Aucune correspondance à contester.");
        var report = await _reports.GetAsync(reportId);
        if (report is not null && ReportStatusRules.CanTransition(report.Status, ReportStatus.MatchDisputed))
        {
            await _reports.UpdateStatusAsync(reportId, ReportStatus.MatchDisputed);
        }
        match.IsDisputed = true;
        match.DisputeReason = reason;
    }
}
