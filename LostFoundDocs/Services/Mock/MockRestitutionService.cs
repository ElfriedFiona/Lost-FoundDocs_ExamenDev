using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// SIMULATION (frontend uniquement) : organisation de la remise, confirmation et reçu fictif.
/// Le reçu n'est pas vérifiable ; le QR code affiché est un dessin local.
/// </summary>
public sealed class MockRestitutionService : IRestitutionService
{
    private readonly IReportService _reports;
    private readonly IAuthService _auth;
    private readonly DemoWorld _world;
    private readonly IConversationService _conversations;
    private readonly INotificationService _notifications;

    public MockRestitutionService(IReportService reports, IAuthService auth, DemoWorld world,
                                  IConversationService conversations, INotificationService notifications)
    {
        _reports = reports;
        _auth = auth;
        _world = world;
        _conversations = conversations;
        _notifications = notifications;
    }

    public RestitutionProposal? GetProposal(Guid reportId)
    {
        lock (_world.Gate) { return _world.Current.Proposals.GetValueOrDefault(reportId); }
    }

    public Receipt? GetReceipt(Guid reportId)
    {
        lock (_world.Gate) { return _world.Current.Receipts.GetValueOrDefault(reportId); }
    }

    public async Task<RestitutionProposal> ProposeAsync(Guid reportId, MeetingPlace place, DateTime slot)
    {
        if (slot <= Clock.Now) throw new ArgumentException("Le créneau doit être dans le futur.", nameof(slot));
        var report = await RequireReport(reportId);
        if (report.Status is not (ReportStatus.MatchFound or ReportStatus.MatchConfirmed))
            throw new InvalidOperationException("La restitution n'est possible qu'après une correspondance.");

        // Le propriétaire qui contre-propose est considéré comme ayant accepté (SIMULATION : l'autre partie suit).
        var proposal = new RestitutionProposal
        {
            ReportId = reportId, Place = place.Name, District = place.District, Slot = slot,
            ProposedByUser = true, Accepted = report.Kind == ReportKind.Lost,
        };
        lock (_world.Gate) { _world.Current.Proposals[reportId] = proposal; }

        if (report.Status == ReportStatus.MatchFound) await _reports.UpdateStatusAsync(reportId, ReportStatus.MatchConfirmed);
        await SystemMessage(reportId, $"Vous avez proposé un rendez-vous : {place.Name} ({place.District}), le {proposal.SlotLabel}.");
        return proposal;
    }

    public async Task<RestitutionProposal> SimulateCounterpartProposalAsync(Guid reportId)
    {
        var report = await RequireReport(reportId);
        if (report.Status != ReportStatus.MatchFound)
            throw new InvalidOperationException("Aucune proposition possible dans l'état actuel.");

        var place = RestitutionRules.Places[0];
        var slot = RestitutionRules.Combine(RestitutionRules.Days(Clock.Today)[0].Date, "10:00");
        var proposal = new RestitutionProposal
        {
            ReportId = reportId, Place = place.Name, District = place.District, Slot = slot,
            ProposedByUser = false, Accepted = false,
        };
        lock (_world.Gate) { _world.Current.Proposals[reportId] = proposal; }
        var who = _conversations.GetForReport(reportId)?.CounterpartName ?? "L’autre personne";
        await SystemMessage(reportId, $"{who} a proposé un rendez-vous : {place.Name} ({place.District}), le {proposal.SlotLabel}.");
        return proposal;
    }

    public async Task AcceptAsync(Guid reportId)
    {
        var proposal = GetProposal(reportId) ?? throw new InvalidOperationException("Aucune proposition à accepter.");
        var report = await RequireReport(reportId);
        proposal.Accepted = true;
        if (report.Status == ReportStatus.MatchFound) await _reports.UpdateStatusAsync(reportId, ReportStatus.MatchConfirmed);
        await SystemMessage(reportId, "Vous avez accepté le rendez-vous.");
    }

    public async Task<Receipt> ConfirmAsync(Guid reportId)
    {
        var report = await RequireReport(reportId);
        if (report.Status != ReportStatus.MatchConfirmed)
            throw new InvalidOperationException("La restitution ne peut être confirmée qu'après l'organisation du rendez-vous.");

        var match = _world.Current.Matches.GetValueOrDefault(reportId) ?? throw new InvalidOperationException("Correspondance introuvable.");
        await _reports.UpdateStatusAsync(reportId, ReportStatus.Returned);

        var me = NameFormat.Short(_auth.CurrentUser?.FullName ?? string.Empty);
        var userIsOwner = report.Kind == ReportKind.Lost;
        var receipt = new Receipt
        {
            ReportId = reportId,
            Reference = report.Reference,
            DocumentTitle = report.Title,
            ReturnedAt = Clock.Now,
            City = report.City,
            OwnerName = userIsOwner ? me : match.CounterpartName,
            HandedByName = userIsOwner ? match.CounterpartName : me,
            Score = match.Score,
            VerificationCode = Receipt.BuildCode(report.Reference),
        };
        lock (_world.Gate) { _world.Current.Receipts[reportId] = receipt; }

        var conversation = _conversations.GetForReport(reportId);
        if (conversation is not null)
        {
            await _conversations.AddSystemMessageAsync(conversation.Id, $"Restitution confirmée · Réf. {report.Reference}");
            await _conversations.CloseAsync(conversation.Id);
        }
        await _notifications.AddAsync(NotificationKind.Restitution,
            $"La restitution de votre {report.Title.ToLowerInvariant()} a été enregistrée.", $"Référence : {report.Reference}", reportId, conversation?.Id);
        return receipt;
    }

    private async Task<Report> RequireReport(Guid id) =>
        await _reports.GetAsync(id) ?? throw new KeyNotFoundException("Signalement introuvable.");

    private async Task SystemMessage(Guid reportId, string text)
    {
        var c = _conversations.GetForReport(reportId);
        if (c is not null) await _conversations.AddSystemMessageAsync(c.Id, text);
    }
}
