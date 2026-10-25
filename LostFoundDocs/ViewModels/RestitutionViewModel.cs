using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;
using LostFoundDocs.UI;

namespace LostFoundDocs.ViewModels;

/// <summary>
/// Organisation de la restitution. Côté « j'ai trouvé » : choisir lieu public + créneau et proposer.
/// Côté « j'ai perdu » : recevoir / accepter une proposition (SIMULATION : la proposition de l'autre personne est déclenchée par un bouton de démonstration).
/// </summary>
public sealed class RestitutionViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IReportService _reports;
    private readonly IMatchService _matches;
    private readonly IConversationService _conversations;
    private readonly IRestitutionService _restitution;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private Guid _id;
    private Report? _report;
    private bool _isChoosing;
    private OptionItem? _selectedPlace;
    private string? _selectedDay, _selectedHour;
    private string _choiceError = string.Empty;
    private string _actionError = string.Empty;
    private readonly IReadOnlyList<(string Label, DateTime Date)> _days = RestitutionRules.Days(Clock.Today);

    public RestitutionViewModel(IReportService reports, IMatchService matches, IConversationService conversations,
                                IRestitutionService restitution, INavigationService nav, IDialogService dialogs)
    {
        _reports = reports; _matches = matches; _conversations = conversations;
        _restitution = restitution; _nav = nav; _dialogs = dialogs;

        PlaceOptions = RestitutionRules.Places
            .Select((p, i) => new OptionItem(i.ToString(), p.Name, p.District, Icons.MapPin)).ToList();
        DayOptions = _days.Select(d => d.Label).ToList();
        HourOptions = RestitutionRules.Hours.ToList();

        ProposeCommand = new AsyncCommand(ProposeAsync);
        SimulateProposalCommand = new AsyncCommand(SimulateProposalAsync);
        AcceptCommand = new AsyncCommand(AcceptAsync);
        CounterCommand = new Command(() => { IsChoosing = true; });
        EditCommand = new Command(() => { IsChoosing = true; });
        ConfirmCommand = new AsyncCommand(ConfirmAsync);
        ConversationCommand = new AsyncCommand(OpenConversationAsync);
        ProofCommand = new AsyncCommand(() => _nav.GoToAsync(Routes.Receipt, new Dictionary<string, object> { ["reportId"] = _id }));
    }

    public IList<OptionItem> PlaceOptions { get; }
    public IList<string> DayOptions { get; }
    public IList<string> HourOptions { get; }

    public ICommand ProposeCommand { get; }
    public ICommand SimulateProposalCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand CounterCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand ConversationCommand { get; }
    public ICommand ProofCommand { get; }

    // ----- sélection -----
    public OptionItem? SelectedPlace { get => _selectedPlace; set { if (SetProperty(ref _selectedPlace, value)) ChoiceError = string.Empty; } }
    public string? SelectedDay { get => _selectedDay; set { if (SetProperty(ref _selectedDay, value)) ChoiceError = string.Empty; } }
    public string? SelectedHour { get => _selectedHour; set { if (SetProperty(ref _selectedHour, value)) ChoiceError = string.Empty; } }
    public string ChoiceError { get => _choiceError; private set { if (SetProperty(ref _choiceError, value)) OnPropertyChanged(nameof(HasChoiceError)); } }
    public bool HasChoiceError => !string.IsNullOrEmpty(ChoiceError);
    public string ActionError { get => _actionError; private set { if (SetProperty(ref _actionError, value)) OnPropertyChanged(nameof(HasActionError)); } }
    public bool HasActionError => !string.IsNullOrEmpty(ActionError);
    public bool IsChoosing { get => _isChoosing; private set { if (SetProperty(ref _isChoosing, value)) RaiseAll(); } }

    // ----- contexte -----
    private ReportStatus? Status => _report?.Status;
    private RestitutionProposal? Proposal => _restitution.GetProposal(_id);
    public bool IsFinder => _report?.Kind == ReportKind.Found;
    public string DocumentTitle => _report?.Title ?? string.Empty;
    public string Reference => _report?.Reference ?? string.Empty;
    public string StatusLabel => _report?.StatusLabel ?? string.Empty;
    public BadgeKind StatusKind => _report is null ? BadgeKind.Neutral : Presentation.Kind(_report.Status);
    public string CounterpartName => _matches.GetMatch(_id)?.CounterpartName ?? "l’autre personne";
    public string MatchLine
    {
        get
        {
            var m = _matches.GetMatch(_id);
            return m is null || _report is null ? string.Empty : $"{m.Score} % · {m.CounterpartName} · {DateFormat.Short(_report.CreatedAt)}";
        }
    }
    public string Intro => IsFinder
        ? $"Choisissez un lieu public et un créneau, puis proposez-le à {CounterpartName}."
        : $"Convenez d’un lieu public et d’un créneau avec {CounterpartName} pour récupérer votre document.";

    public bool CanOrganize => Status is ReportStatus.MatchFound or ReportStatus.MatchConfirmed;
    public bool HasProposal => Proposal is not null;
    public bool ShowChoose => CanOrganize && (IsChoosing || (IsFinder && !HasProposal));
    public bool ShowSimulate => CanOrganize && !IsFinder && !HasProposal && !IsChoosing;
    public bool ShowPending => CanOrganize && !IsFinder && Proposal is { ProposedByUser: false, Accepted: false } && !IsChoosing;
    public bool ShowProposalCard => CanOrganize && HasProposal && !IsChoosing;
    public bool ShowConfirm => Status == ReportStatus.MatchConfirmed && HasProposal && !IsChoosing;
    public bool ShowReturned => Status == ReportStatus.Returned;
    public bool ShowUnavailable => _report is not null && !CanOrganize && !ShowReturned;
    public bool CanOpenConversation => _conversations.GetForReport(_id) is not null;
    public bool ShowEdit => IsFinder && ShowConfirm;

    public string ProposalPlace => Proposal is null ? string.Empty : $"{Proposal.Place} ({Proposal.District})";
    public string ProposalSlot => Proposal?.SlotLabel ?? string.Empty;
    public string ProposalOrigin => Proposal is null ? string.Empty
        : Proposal.ProposedByUser ? "Votre proposition" : $"Proposition de {CounterpartName}";
    public string ConfirmText => IsFinder ? "J’ai remis le document" : "J’ai récupéré mon document";
    public string ProposeText => HasProposal ? "Envoyer la nouvelle proposition" : "Proposer ce rendez-vous";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id)
        {
            _id = id;
            _ = LoadAsync();
        }
    }

    public async Task LoadAsync()
    {
        if (_id == Guid.Empty) return;
        _report = await _reports.GetAsync(_id);
        RaiseAll();
    }

    private async Task ProposeAsync()
    {
        ChoiceError = string.Empty;
        if (SelectedPlace is null) { ChoiceError = "Choisissez un lieu public."; return; }
        if (SelectedDay is null || SelectedHour is null) { ChoiceError = "Choisissez un jour et un créneau."; return; }

        var day = _days.First(d => d.Label == SelectedDay).Date;
        var slot = RestitutionRules.Combine(day, SelectedHour);
        var place = RestitutionRules.Places[int.Parse(SelectedPlace.Id)];
        try
        {
            await _restitution.ProposeAsync(_id, place, slot);
            _isChoosing = false;
            await LoadAsync();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ChoiceError = ex.Message;
        }
    }

    private async Task SimulateProposalAsync()
    {
        ActionError = string.Empty;
        try { await _restitution.SimulateCounterpartProposalAsync(_id); }
        catch (InvalidOperationException ex) { ActionError = ex.Message; }
        await LoadAsync();
    }

    private async Task AcceptAsync()
    {
        ActionError = string.Empty;
        try { await _restitution.AcceptAsync(_id); }
        catch (InvalidOperationException ex) { ActionError = ex.Message; }
        await LoadAsync();
    }

    private async Task ConfirmAsync()
    {
        var message = IsFinder
            ? "Cette action indique que le document a effectivement été remis à son propriétaire."
            : "Cette action indique que vous avez bien récupéré votre document.";
        if (!await _dialogs.ConfirmAsync("Confirmer la restitution ?", message, "Confirmer", "Annuler")) return;
        ActionError = string.Empty;
        try
        {
            await _restitution.ConfirmAsync(_id);
            await _nav.GoToAsync("../" + Routes.Restituted, new Dictionary<string, object> { ["reportId"] = _id });
        }
        catch (InvalidOperationException ex)
        {
            ActionError = ex.Message;
        }
    }

    private async Task OpenConversationAsync()
    {
        if (_conversations.GetForReport(_id) is { } c)
            await _nav.GoToAsync(Routes.Conversation, new Dictionary<string, object> { ["conversationId"] = c.Id });
    }
}
