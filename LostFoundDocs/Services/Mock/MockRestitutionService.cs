using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.Services.Mock;

// SIMULATION (frontend uniquement) : rendez-vous et reçus non vérifiables.
public sealed class MockRestitutionService(IReportService reports, IMatchService matches,
    IConversationService conversations, INotificationService notifications, IAuthService auth) : IRestitutionService
{
    private readonly Dictionary<Guid, RestitutionProposal> _proposals = new();
    private readonly Dictionary<Guid, Receipt> _receipts = new();
    public async Task<RestitutionProposal?> GetProposalAsync(Guid reportId)
    {
        var report = await RequireReport(reportId);
        if (report.Status is not (ReportStatus.MatchFound or ReportStatus.MatchConfirmed))
            throw new InvalidOperationException("Cette correspondance ne permet pas de rendez-vous.");
        if (!_proposals.ContainsKey(reportId) && report.Kind == ReportKind.Lost)
            _proposals[reportId] = new("Entrée principale, Université de Douala – Campus 1", Clock.Today.AddDays(1).AddHours(10), false);
        return _proposals.GetValueOrDefault(reportId);
    }
    public async Task ProposeAsync(Guid reportId, string place, DateTime appointment)
    {
        var report = await RequireReport(reportId);
        if (report.Status is not (ReportStatus.MatchFound or ReportStatus.MatchConfirmed))
            throw new InvalidOperationException("Cette correspondance ne permet pas de rendez-vous.");
        if (string.IsNullOrWhiteSpace(place) || appointment <= Clock.Now)
            throw new ArgumentException("Choisissez un lieu et un créneau futur.");
        _proposals[reportId] = new(place, appointment, report.Kind == ReportKind.Found);
        await reports.UpdateStatusAsync(reportId, ReportStatus.MatchConfirmed);
        await conversations.AddSystemAsync(reportId, "Rendez-vous proposé : " + _proposals[reportId].Summary);
        notifications.Add(report, "Proposition de restitution · " + report.Title, Routes.Restitution);
    }
    public async Task AcceptAsync(Guid reportId)
    {
        var proposal = await GetProposalAsync(reportId) ?? throw new InvalidOperationException("Aucune proposition reçue.");
        if (proposal.Appointment <= Clock.Now) throw new InvalidOperationException("Proposition expirée : choisissez un autre créneau.");
        await reports.UpdateStatusAsync(reportId, ReportStatus.MatchConfirmed);
        _proposals[reportId] = proposal with { Accepted = true };
        await conversations.AddSystemAsync(reportId, "Rendez-vous accepté : " + proposal.Summary);
    }
    public async Task<Receipt> ConfirmAsync(Guid reportId)
    {
        var report = await RequireReport(reportId);
        if (report.Status == ReportStatus.Returned) return await GetReceiptAsync(reportId);
        var proposal = await GetProposalAsync(reportId);
        if (proposal is not { Accepted: true }) throw new InvalidOperationException("Acceptez d’abord un rendez-vous.");
        await reports.UpdateStatusAsync(reportId, ReportStatus.Returned);
        var receipt = await GetReceiptAsync(reportId);
        await conversations.AddSystemAsync(reportId, "Restitution confirmée · " + report.Reference);
        notifications.Add(report, "Document restitué · " + report.Title, Routes.Receipt);
        return receipt;
    }
    public async Task<Receipt> GetReceiptAsync(Guid reportId)
    {
        var report = await RequireReport(reportId);
        if (report.Status != ReportStatus.Returned) throw new InvalidOperationException("Aucune restitution confirmée.");
        if (_receipts.TryGetValue(reportId, out var existing)) return existing;
        var match = await matches.GetAsync(reportId) ?? throw new InvalidOperationException("Correspondance introuvable.");
        var user = auth.CurrentUser ?? throw new InvalidOperationException("Session expirée.");
        var receipt = new Receipt(report, report.Kind == ReportKind.Lost ? user.FullName : match.Contact,
            report.Kind == ReportKind.Found ? user.FullName : match.Contact, report.UpdatedAt, match.Score);
        _receipts[reportId] = receipt;
        return receipt;
    }
    private async Task<Report> RequireReport(Guid id) => await reports.GetAsync(id)
        ?? throw new InvalidOperationException("Signalement introuvable pour cette session.");
}
