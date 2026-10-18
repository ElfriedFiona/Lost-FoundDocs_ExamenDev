using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class ReportDetailsViewModel : ReportContextViewModel
{
    public ReportDetailsViewModel(IReportService reports, INavigationService nav, IDialogService dialogs) : base(reports, nav)
    {
        SearchCommand = new AsyncCommand(() => OpenAsync(Routes.Match));
        ConversationCommand = new AsyncCommand(() => OpenAsync(Routes.Conversation));
        RestitutionCommand = new AsyncCommand(() => OpenAsync(Routes.Restitution));
        ReceiptCommand = new AsyncCommand(() => OpenAsync(Routes.Receipt));
        ListCommand = new AsyncCommand(() => nav.GoToAsync("//" + Routes.Reports));
        HomeCommand = new AsyncCommand(() => nav.GoToAsync("//" + Routes.Home));
        CancelCommand = new AsyncCommand(async () =>
        {
            if (!await dialogs.ConfirmAsync("Annuler le signalement ?", "La recherche sera arrêtée pour ce document.", "Annuler le signalement", "Conserver")) return;
            if (!await reports.CancelAsync(ReportId)) throw new InvalidOperationException("Ce signalement ne peut plus être annulé.");
            OnPropertyChanged(string.Empty);
        });
    }
    public ICommand SearchCommand { get; }
    public ICommand ConversationCommand { get; }
    public ICommand RestitutionCommand { get; }
    public ICommand ReceiptCommand { get; }
    public ICommand ListCommand { get; }
    public ICommand HomeCommand { get; }
    public ICommand CancelCommand { get; }
    public bool CanSearch => Report?.Status == ReportStatus.Searching;
    public bool HasMatch => Report?.Status is ReportStatus.MatchFound or ReportStatus.MatchConfirmed or ReportStatus.MatchDisputed;
    public bool CanContact => Report?.Status is ReportStatus.MatchFound or ReportStatus.MatchConfirmed;
    public bool IsReturned => Report?.Status == ReportStatus.Returned;
    public bool CanCancel => Report is { IsTerminal: false };
    public string Timeline => Report is null ? "" : Report.History.Count == 0
        ? $"Déclaré le {Report.CreatedAt:dd/MM/yyyy HH:mm}\n{Report.StatusLabel} · {Report.UpdatedAt:dd/MM/yyyy HH:mm}"
        : string.Join("\n", Report.History.Select(change => $"{ReportStatusRules.Label(change.Status)} · {change.ChangedAt:dd/MM/yyyy HH:mm}"));
    public bool HasPhoto => Report?.PhotoPath is not null;
}
