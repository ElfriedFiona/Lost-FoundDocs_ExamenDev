using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;
using LostFoundDocs.UI;

namespace LostFoundDocs.ViewModels;

public sealed record CriterionRow(string Name, string StateLabel, BadgeKind Kind, bool IsMatch);

/// <summary>Socle des écrans « Correspondance trouvée » et « Détails de la correspondance » (carte, critères, signalement d'erreur).</summary>
public abstract class MatchViewModelBase : ViewModelBase, IQueryAttributable
{
    protected readonly IReportService Reports;
    protected readonly IMatchService Matches;
    protected readonly IConversationService Conversations;
    protected readonly INavigationService Nav;
    protected Report? Report;
    protected MatchResult? Match;

    private bool _isExplanationOpen;
    private bool _isReasonOpen;
    private OptionItem? _selectedReason;
    private string _reasonError = string.Empty;

    protected MatchViewModelBase(IReportService reports, IMatchService matches, IConversationService conversations, INavigationService nav, bool explanationOpen)
    {
        Reports = reports;
        Matches = matches;
        Conversations = conversations;
        Nav = nav;
        _isExplanationOpen = explanationOpen;
        ToggleExplanationCommand = new Command(() => { IsExplanationOpen = !IsExplanationOpen; OnPropertyChanged(nameof(ExplanationGlyph)); });
        ContactCommand = new AsyncCommand(ContactAsync);
        ReportErrorCommand = new Command(() => { SelectedReason = null; ReasonError = string.Empty; IsReasonOpen = true; });
        CancelReasonCommand = new Command(() => IsReasonOpen = false);
        SendReasonCommand = new AsyncCommand(SendReasonAsync);
        CloseCommand = new AsyncCommand(() => Nav.GoToAsync("//" + Routes.Home));
    }

    public ICommand ToggleExplanationCommand { get; }
    public ICommand ContactCommand { get; }
    public ICommand ReportErrorCommand { get; }
    public ICommand CancelReasonCommand { get; }
    public ICommand SendReasonCommand { get; }
    public ICommand CloseCommand { get; }

    public IList<OptionItem> ReasonOptions { get; } = new List<OptionItem>
    {
        new("not-mine", "Ce n’est pas mon document"),
        new("mismatch", "Les informations ne correspondent pas"),
        new("recovered", "J’ai déjà récupéré mon document"),
    };

    public bool IsReasonOpen { get => _isReasonOpen; set => SetProperty(ref _isReasonOpen, value); }
    public OptionItem? SelectedReason { get => _selectedReason; set { if (SetProperty(ref _selectedReason, value)) ReasonError = string.Empty; } }
    public string ReasonError { get => _reasonError; set { if (SetProperty(ref _reasonError, value)) OnPropertyChanged(nameof(HasReasonError)); } }
    public bool HasReasonError => !string.IsNullOrEmpty(ReasonError);

    public bool IsExplanationOpen { get => _isExplanationOpen; set => SetProperty(ref _isExplanationOpen, value); }
    public string ExplanationGlyph => IsExplanationOpen ? Icons.ChevronDown : Icons.ChevronRight;

    // ----- contenu de la carte -----
    public bool HasMatch => Match is not null;
    public bool IsOwnerSide => Report?.Kind == ReportKind.Lost;
    public string IconGlyph => Report is null ? Icons.Award : Presentation.Icon(Report.Type);
    public string Title => Report?.Title ?? string.Empty;
    public string ReferenceLabel => Match is null ? string.Empty : $"Réf. {Match.CounterpartReference}";
    public string ScoreText => Match is null ? string.Empty : $"{Match.Score} %";
    public string ScoreDescription => Match is null ? string.Empty : $"Score de correspondance : {Match.Score} pour cent, {Match.Strength}";
    public double ScoreFraction => Match is null ? 0 : Match.Score / 100.0;
    public string StrengthLabel => Match?.Strength ?? string.Empty;
    public string CityLabel => Match?.City ?? string.Empty;
    public string DateCaption => IsOwnerSide ? "Date de découverte" : "Date de perte déclarée";
    public string DateValue => Match is null ? string.Empty : DateFormat.Short(Match.CounterpartDate);

    public IList<CriterionRow> Criteria => Match?.Criteria.Select(c => c.State switch
    {
        MatchState.Match => new CriterionRow(c.Name, "Concordant", BadgeKind.Success, true),
        MatchState.Partial => new CriterionRow(c.Name, "Partiel", BadgeKind.Warning, false),
        _ => new CriterionRow(c.Name, "Non comparé", BadgeKind.Neutral, false),
    }).ToList() ?? new List<CriterionRow>();

    // ----- état de la correspondance -----
    public bool IsDisputed => Match?.IsDisputed ?? false;
    public bool CanReportError => Match is not null && !IsDisputed && Report?.Status == ReportStatus.MatchFound;
    public string ContactText => IsOwnerSide ? "Contacter la personne qui l’a trouvé" : "Contacter le propriétaire";
    public string DisputeMessage => Match?.DisputeReason == "J’ai déjà récupéré mon document"
        ? "Merci. Vous pourrez clôturer ce signalement depuis son détail. La recherche automatique continue."
        : "Notre équipe va vérifier cette correspondance. La recherche automatique continue.";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id) _ = LoadAsync(id);
    }

    protected virtual async Task LoadAsync(Guid id)
    {
        Report = await Reports.GetAsync(id);
        Match = Matches.GetMatch(id);
        RaiseAll();
    }

    protected async Task ContactAsync()
    {
        if (Report is null || Match is null) return;
        var conversation = Conversations.GetForReport(Report.Id) ?? await Conversations.EnsureForMatchAsync(Report, Match);
        await Nav.GoToAsync(Routes.Conversation, new Dictionary<string, object> { ["conversationId"] = conversation.Id });
    }

    private async Task SendReasonAsync()
    {
        if (SelectedReason is null)
        {
            ReasonError = "Choisissez un motif pour continuer.";
            return;
        }
        if (Report is null) return;
        await Matches.ReportErrorAsync(Report.Id, SelectedReason.Title);
        IsReasonOpen = false;
        Report = await Reports.GetAsync(Report.Id);
        RaiseAll();
    }
}
