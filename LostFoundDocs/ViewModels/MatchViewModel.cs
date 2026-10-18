using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class MatchViewModel : ReportContextViewModel
{
    private readonly IMatchService _matches;
    private readonly IDialogService _dialogs;
    private bool _showCriteria;
    private bool _searchComplete;
    public MatchResult? Match { get; private set; }
    public MatchViewModel(IReportService reports, INavigationService nav, IMatchService matches, IDialogService dialogs) : base(reports, nav)
    {
        _matches = matches; _dialogs = dialogs;
        ContactCommand = new AsyncCommand(() => OpenAsync(Routes.Conversation));
        DetailsCommand = new AsyncCommand(() => OpenAsync(Routes.MatchDetails));
        ReportCommand = new AsyncCommand(() => OpenAsync(Routes.ReportDetails));
        HomeCommand = new AsyncCommand(() => nav.GoToAsync("//" + Routes.Home));
        DisputeCommand = new AsyncCommand(DisputeAsync);
        ToggleCriteriaCommand = new Command(() => { _showCriteria = !_showCriteria; OnPropertyChanged(nameof(ShowCriteria)); });
    }
    public ICommand ContactCommand { get; }
    public ICommand DetailsCommand { get; }
    public ICommand ReportCommand { get; }
    public ICommand HomeCommand { get; }
    public ICommand DisputeCommand { get; }
    public ICommand ToggleCriteriaCommand { get; }
    public bool ShowCriteria => _showCriteria;
    public bool HasMatch => Match is not null;
    public bool IsFound => Report?.Kind == ReportKind.Found;
    public bool IsLost => !IsFound;
    public bool CanContactFromResult => IsFound && CanContact;
    public bool NoMatch => _searchComplete && Match is null && Report?.Status == ReportStatus.Searching;
    public bool IsUnavailable => _searchComplete && Match is null && Report?.Status != ReportStatus.Searching;
    public bool IsDisputed => Report?.Status == ReportStatus.MatchDisputed;
    public bool CanDispute => Report?.Status == ReportStatus.MatchFound;
    public bool CanContact => Report?.Status is ReportStatus.MatchFound or ReportStatus.MatchConfirmed;
    public string Title => !_searchComplete ? "Recherche de correspondance" : HasMatch ? "Correspondance trouvée" : "Aucun résultat";
    public string ContactText => Report?.Kind == ReportKind.Lost ? "Contacter le trouveur" : "Contacter le propriétaire";
    public string Criteria => $"Type : concordant\nVille : concordante (simulation)\nNom : {(string.IsNullOrEmpty(Report?.HolderName) ? "non comparé" : "partiel")}\nNuméro : {(string.IsNullOrEmpty(Report?.Number) ? "non comparé" : "concordant (simulation)")}\nDate : chronologie compatible\nNaissance et organisme : non comparés";
    protected override async Task LoadAsync()
    {
        Match = Report?.Status == ReportStatus.Searching ? await _matches.SearchAsync(ReportId) : await _matches.GetAsync(ReportId);
        _searchComplete = true;
    }
    private async Task DisputeAsync()
    {
        var reason = await _dialogs.ChooseAsync("Signaler une erreur", "Annuler", "Ce n’est pas mon document",
            "Les informations ne correspondent pas", "J’ai déjà récupéré mon document");
        if (reason is null or "Annuler") return;
        await _matches.DisputeAsync(ReportId, reason);
        if (reason == "J’ai déjà récupéré mon document" && await _dialogs.ConfirmAsync("Clôturer le signalement ?", "Confirmez-vous avoir récupéré votre document ?", "Confirmer"))
        {
            await Reports.UpdateStatusAsync(ReportId, ReportStatus.Returned);
            await OpenAsync(Routes.Receipt);
        }
        OnPropertyChanged(string.Empty);
    }
}
